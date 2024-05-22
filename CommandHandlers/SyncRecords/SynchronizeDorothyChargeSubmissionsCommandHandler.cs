using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Utils.MultiThreading;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.Redash;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Enums.SyncRecords;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;
using WildHealth.Redash.Clients.Models;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class SynchronizeDorothyChargeSubmissionsCommandHandler : IRequestHandler<SynchronizeDorothyChargeSubmissionsCommand>
{
    private readonly ILogger<SynchronizeDorothyChargeSubmissionsCommandHandler> _logger;
    private readonly IRedashService _redashService;
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IRunInParallelUtility<FhirChargeSubmissionModel, SynchronizeDorothyChargeSubmissionsCommand> _runInParallelUtility;
    private readonly IServiceProvider _services;

    public SynchronizeDorothyChargeSubmissionsCommandHandler(
        ILogger<SynchronizeDorothyChargeSubmissionsCommandHandler> logger,
        IRedashService redashService,
        ISyncRecordsService syncRecordsService,
        IRunInParallelUtility<FhirChargeSubmissionModel, SynchronizeDorothyChargeSubmissionsCommand> runInParallelUtility,
        IServiceProvider services)
    {
        _logger = logger;
        _redashService = redashService;
        _syncRecordsService = syncRecordsService;
        _runInParallelUtility = runInParallelUtility;
        _services = services;
    }

    public async Task Handle(SynchronizeDorothyChargeSubmissionsCommand command, CancellationToken cancellationToken)
    {
        await _runInParallelUtility.Run( 
            shardSize: command.ShardSize,
            executeFunction: Sync,
            sourceFunction: GetFhirChargeSubmissions,
            command: command,
            maxRecords: null
        );
    }
    
    #region private

    private async Task Sync(FhirChargeSubmissionModel[] charges, SynchronizeDorothyChargeSubmissionsCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Synchronizing of dorothy charge submissions has: started");

        var scope = _services.CreateScope();
        var scopedSyncRecordsService = scope.ServiceProvider.GetRequiredService<ISyncRecordsService>();
        var scopedEventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var scopedMapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        foreach (var charge in charges)
        {
            var keys = new Dictionary<string, string>()
            {
                {nameof(SyncRecordFhirCharge.EncounterId), charge.EncounterId.ToString()}
            };
            
            var existingClaimRecords = await scopedSyncRecordsService.GetByKeys<SyncRecordFhirCharge>(keys);

            if (!existingClaimRecords.IsNullOrEmpty())
            {
                foreach (var existingRecord in existingClaimRecords)
                {
                    await UpdateExistingRecord(existingRecord, charge, scopedSyncRecordsService);
                }

                continue;
            }

            await CreateNewRecord(charge, command.PracticeId, scopedSyncRecordsService, scopedMapper);

            await PublishFhirClaimSubmissionIntegrationEvent(charge, scopedEventBus);
        }

        _logger.LogInformation("Synchronizing of dorothy charge submissions has: finished");
    }

    private async Task<FhirChargeSubmissionModel[]> GetFhirChargeSubmissions(SynchronizeDorothyChargeSubmissionsCommand command, int? numberOfRecords)
    {
        var lastCreatedRecord = await _syncRecordsService.GetMostRecentRecordByDatum<SyncRecordFhirCharge>(
            SyncRecordType.FhirCharge, 
            SyncRecordStatus.FhirChargeSubmitted,
            nameof(SyncRecordFhirCharge.CreatedDate),
            practiceId: command.PracticeId);

        var startDate = lastCreatedRecord?.CreatedDate ?? DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;
        
        var results = await _redashService.QueryFhirChargeSubmissionsAsync(startDate, endDate, command.PracticeId);
        return results.Data.Rows.ToArray();
    }

    private async Task UpdateExistingRecord(
        SyncRecordFhirCharge record, 
        FhirChargeSubmissionModel charge,
        ISyncRecordsService scopedSyncRecordsService)
    {
        record.Facility = charge.Facility;
        record.CptId = charge.CptId;
        record.CreatedDate = charge.CreatedDate;
        record.EncounterId = charge.EncounterId;
        record.InsurerId = charge.InsurerId;
        record.PatientId = charge.PatientId;
        record.TotalAmount = charge.TotalAmount;

        record.SyncRecord.Status = SyncRecordStatus.FhirChargeSubmitted;
        
        await scopedSyncRecordsService.UpdateAsync(record);
    }

    private async Task CreateNewRecord(
        FhirChargeSubmissionModel charge,
        int practiceId,
        ISyncRecordsService scopedSyncRecordsService,
        IMapper scopedMapper)
    {
        var record = scopedMapper.Map<SyncRecordFhirCharge>(charge);
        await scopedSyncRecordsService.CreateAsync(
            syncRecord: record,
            type: SyncRecordType.FhirCharge,
            practiceId: practiceId,
            status: SyncRecordStatus.FhirChargeSubmitted);
    }

    private async Task PublishFhirClaimSubmissionIntegrationEvent(
        FhirChargeSubmissionModel charge,
        IEventBus scopedEventBus)
    {
        var payload = new FhirChargeCreatedPayload(
            id: charge.EncounterId.ToString(),
            patientId: charge.PatientId.ToString(),
            insurerId: charge.InsurerId,
            totalAmount: charge.TotalAmount.ToString(),
            facility: charge.Facility,
            cptCode: charge.CptId,
            dateOfService: charge.DosFrom.ToString("d"));

        await scopedEventBus.Publish(new FhirChargeIntegrationEvent(payload, DateTime.UtcNow));
    }

    #endregion
}
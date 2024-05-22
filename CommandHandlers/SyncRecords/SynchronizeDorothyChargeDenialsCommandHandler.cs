using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
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

public class SynchronizeDorothyChargeDenialsCommandHandler : IRequestHandler<SynchronizeDorothyChargeDenialsCommand>
{
    private readonly ILogger<SynchronizeDorothyChargeDenialsCommandHandler> _logger;
    private readonly IRedashService _redashService;
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;

    public SynchronizeDorothyChargeDenialsCommandHandler(
        ILogger<SynchronizeDorothyChargeDenialsCommandHandler> logger,
        IRedashService redashService,
        ISyncRecordsService syncRecordsService,
        IMapper mapper,
        IEventBus eventBus)
    {
        _logger = logger;
        _redashService = redashService;
        _syncRecordsService = syncRecordsService;
        _mapper = mapper;
        _eventBus = eventBus;
    }

    public async Task Handle(SynchronizeDorothyChargeDenialsCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Synchronizing of dorothy charge denials has: started");

        var lastCreatedRecord = await _syncRecordsService.GetMostRecentRecordByDatum<SyncRecordFhirCharge>(
            type: SyncRecordType.FhirCharge,
            status: SyncRecordStatus.FhirChargeDenied,
            key: nameof(SyncRecordFhirCharge.DeniedDate),
            practiceId: command.PracticeId);

        var startDate = lastCreatedRecord?.DeniedDate ?? DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;
        
        var results = await _redashService.QueryFhirChargeDenialsAsync(startDate, endDate, command.PracticeId);
        var charges = results.Data.Rows;
        
        foreach (var charge in charges)
        {
            var keys = new Dictionary<string, string>()
            {
                {nameof(SyncRecordFhirCharge.EncounterId), charge.EncounterId.ToString()}
            };
            
            var existingClaimRecords = await _syncRecordsService.GetByKeys<SyncRecordFhirCharge>(
                keys: keys,
                practiceId: command.PracticeId);

            if (!existingClaimRecords.IsNullOrEmpty())
            {
                foreach (var existingRecord in existingClaimRecords)
                {
                    await UpdateExistingRecord(existingRecord, charge);
                }

                continue;
            }

            await CreateNewRecord(charge, command.PracticeId);

            await PublishFhirClaimSubmissionIntegrationEvent(charge);
        }
 
        _logger.LogInformation("Synchronizing of dorothy charge denials has: finished");
    }

    #region private

    private async Task UpdateExistingRecord(
        SyncRecordFhirCharge record,
        FhirChargeDenialModel charge)
    {
        record.Facility = charge.Facility;
        record.CptId = charge.CptId;
        record.CreatedDate = charge.CreatedDate;
        record.EncounterId = charge.EncounterId;
        record.InsurerId = charge.InsurerId;
        record.PatientId = charge.PatientId;
        record.TotalAmount = charge.TotalAmount;
        record.DeniedDate = charge.DeniedDate;

        record.SyncRecord.Status = SyncRecordStatus.FhirChargeDenied;
        
        await _syncRecordsService.UpdateAsync(record);
    }

    private async Task CreateNewRecord(FhirChargeDenialModel charge, int practiceId)
    {
        var record = _mapper.Map<SyncRecordFhirCharge>(charge);
        await _syncRecordsService.CreateAsync(
            syncRecord: record,
            type: SyncRecordType.FhirCharge,
            practiceId: practiceId,
            status: SyncRecordStatus.FhirChargeDenied);
    }

    private async Task PublishFhirClaimSubmissionIntegrationEvent(FhirChargeDenialModel charge)
    {
        var payload = new FhirChargeDeniedPayload(
            id: charge.EncounterId.ToString(),
            patientId: charge.PatientId.ToString(),
            insurerId: charge.InsurerId,
            totalAmount: charge.TotalAmount.ToString(),
            facility: charge.Facility,
            cptCode: charge.CptId,
            dateOfService: charge.DosFrom.ToString("d"));

        await _eventBus.Publish(new FhirChargeIntegrationEvent(payload, DateTime.UtcNow));
    }

    #endregion
}
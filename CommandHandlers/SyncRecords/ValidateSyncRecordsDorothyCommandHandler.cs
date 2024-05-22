using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Maps;
using WildHealth.Application.Utils.MultiThreading;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Enums.SyncRecords;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.States;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.SyncRecords;
using WildHealth.Lob.Clients.Exceptions;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class ValidateSyncRecordsDorothyCommandHandler : IRequestHandler<ValidateSyncRecordsDorothyCommand>
{    
    private readonly SyncRecordStatus[] _duplicateAlreadyAccountedForStatuses = new[]
    {
        SyncRecordStatus.ReadyForSync, 
        SyncRecordStatus.SyncComplete, 
        SyncRecordStatus.FailedSync,
        SyncRecordStatus.DorothyRequiresDiscovery
    };
    
    private readonly ILogger<ValidateSyncRecordsDorothyCommandHandler> _logger;
    private readonly IServiceProvider _services;
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IRunInParallelUtility<SyncRecordDorothy, ValidateSyncRecordsDorothyCommand> _runInParallelUtility;

    public ValidateSyncRecordsDorothyCommandHandler(
        ILogger<ValidateSyncRecordsDorothyCommandHandler> logger,
        IServiceProvider services,
        IRunInParallelUtility<SyncRecordDorothy, ValidateSyncRecordsDorothyCommand> runInParallelUtility
        )
    {
        _logger = logger;
        _services = services;
        _runInParallelUtility = runInParallelUtility;
        
        var scope = _services.CreateScope();
        _syncRecordsService = scope.ServiceProvider.GetRequiredService<ISyncRecordsService>();
    }

    public async Task Handle(ValidateSyncRecordsDorothyCommand command, CancellationToken cancellationToken)
    {
        await _runInParallelUtility.Run(
            shardSize: command.ShardSize,
            executeFunction: Validate,
            sourceFunction: GetDorothySyncRecordsToValidate,
            command: command,
            maxRecords: command.NumberOfRecordsToValidate
        );
    }

    #region private

    private async Task Validate(SyncRecordDorothy[] recordsToValidate, ValidateSyncRecordsDorothyCommand command, CancellationToken cancellationToken)
    {
        var scope = _services.CreateScope();
        var scopedSyncRecordsService = scope.ServiceProvider.GetRequiredService<ISyncRecordsService>();
        var scopedTransactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var scopedStatesService = scope.ServiceProvider.GetRequiredService<IStatesService>();

        var priorStatuses = recordsToValidate.ToDictionary(o => o.SyncRecord.GetId(), o => o.SyncRecord.Status);

        ////////////////////////////////////////////////////////////////////////
        // First update all statuses to locked
        ////////////////////////////////////////////////////////////////////////
        await using (var transaction = scopedTransactionManager.BeginTransaction())
        {
            try
            {
                foreach (var recordToValidate in recordsToValidate)
                {
                    var realSyncRecord =
                        await scopedSyncRecordsService.GetById(recordToValidate.SyncRecord.GetId());
                    
                    realSyncRecord.Status = SyncRecordStatus.Locked;

                    await scopedSyncRecordsService.UpdateAsync(realSyncRecord);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                _logger.LogError($"Unable to set all statuses to validating: {priorStatuses.ToString()}");
                    
                await transaction.RollbackAsync(cancellationToken);
            }
        }
        
        ////////////////////////////////////////////////////////////////////////
        // Run the validation
        ////////////////////////////////////////////////////////////////////////
        foreach (var record in recordsToValidate)
        {
            var realSyncRecord =
                await scopedSyncRecordsService.GetById(record.SyncRecord.GetId());

            try
            {
                await ValidateDorothyEntryAndUpdateStatus(
                    scopedSyncRecordsService,
                    scopedMediator,
                    scopedStatesService,
                    record, 
                    realSyncRecord);
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Failed to validate Dorothy sync record with id: {record.SyncRecord.Id}");

                var syncRecord = record?.SyncRecord;
                
                if (e.Message.Contains("was not recognized as a valid DateTime"))
                {
                    realSyncRecord.Status = SyncRecordStatus.DorothyInvalidBirthdayFormat;
                }
                else if (syncRecord?.Id != null)
                {
                    realSyncRecord.Status = priorStatuses[syncRecord.GetId()];
                }

                await scopedSyncRecordsService.UpdateAsync(realSyncRecord);
            }
        }
    }

    private async Task<SyncRecordDorothy[]> GetDorothySyncRecordsToValidate (ValidateSyncRecordsDorothyCommand command, int? numberOfRecordsToValidate)
    {
        var statusesToValidate = new[]
        {
            SyncRecordStatus.PendingValidation,
            // SyncRecordStatus.DorothyDuplicatePolicyId,
            // SyncRecordStatus.DorothyDuplicateBirthdateZipCode,
            // SyncRecordStatus.DorothyRequiresDiscovery
        };

        return await _syncRecordsService.GetByTypeAndStatus<SyncRecordDorothy>(
            type: SyncRecordType.Dorothy,
            statuses: statusesToValidate,
            count: numberOfRecordsToValidate ?? 500,
            isTracking: false);
    }

    private async Task ValidateDorothyEntryAndUpdateStatus(
        ISyncRecordsService scopedSyncRecordsService, 
        IMediator scopedMediator,
        IStatesService scopedStatesService,
        SyncRecordDorothy record, 
        SyncRecord realSyncRecord)
    {
        realSyncRecord.Status = await GetRecordStatus(scopedSyncRecordsService, scopedMediator, scopedStatesService, record);

        if (realSyncRecord.Status == SyncRecordStatus.ReadyForSync)
        {
            record.PolicyCarrier = OpenPmConstants.Organization.Medicare;
            record.SetStatus(realSyncRecord.Status);
            
            await scopedSyncRecordsService.UpdateAsync(record);
        }
        else
        {
            await scopedSyncRecordsService.UpdateAsync(realSyncRecord);
        }
    }

    private async Task<SyncRecordStatus> GetRecordStatus(
        ISyncRecordsService scopedSyncRecordsService,
        IMediator scopedMediator,
        IStatesService scopedStatesService,
        SyncRecordDorothy record)
    {
        if (string.IsNullOrEmpty(record.PolicyId))
        {
            return SyncRecordStatus.DorothyRequiresDiscovery;
        }
        
        var potentialDuplicatePolicy = await scopedSyncRecordsService.GetByDuplicatePolicyId(record);

        if (!potentialDuplicatePolicy.IsNullOrEmpty())
        {
            // if one is already through validation and sync processes or we have one in discovery already, then send this to discarded
            if (potentialDuplicatePolicy.Any(o => _duplicateAlreadyAccountedForStatuses.Contains(o.SyncRecord.Status)))
            {
                return SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded;
            }
            
            return SyncRecordStatus.DorothyDuplicatePolicyId;
        }

        if (IsInvalidAddress(record))
        {
            return SyncRecordStatus.DorothyInvalidAddress;
        }

        if (IsInvalidName(record))
        {
            return SyncRecordStatus.DorothyInvalidName;
        }

        try
        {
            var isShippableAddress = await IsShippableAddress(
                scopedMediator,
                scopedStatesService,
                record);

            if (!isShippableAddress)
            {
                return SyncRecordStatus.DorothyUnshippableAddress;
            }
        }
        catch (LobException e) when (e.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            return SyncRecordStatus.DorothyInvalidAddress;
        }
        

        try
        {
            var birthday = DateTime.Parse(record.Birthday);

            if (birthday.Year > 2004)
            {
                return SyncRecordStatus.DorothyInvalidBirthday;
            }
        }
        catch (Exception)
        {
            return SyncRecordStatus.DorothyInvalidBirthday;
        }

        var potentialDuplicateBirthdayZipFirstLast = await scopedSyncRecordsService.GetByDuplicateBirthdayZipFirstLast(record);
        
        if (potentialDuplicateBirthdayZipFirstLast is not null)
        {
            return SyncRecordStatus.DorothyDuplicateBirthdateZipCodeFirstLast;
        }
        
        if (IsInvalidPolicyId(record))
        {
            return SyncRecordStatus.DorothyRequiresDiscovery;
        }

        return SyncRecordStatus.ReadyForSync;
    }

    private bool IsInvalidPolicyId(SyncRecordDorothy record) => !HasProperStructure(record.PolicyId);

    private bool HasProperStructure(string? source) => 
        !string.IsNullOrEmpty(source) &&
        Regex.IsMatch(source,@"\b[1-9](?![sloibzSLOIBZ])[a-zA-Z](?![sloibzSLOIBZ)])[a-zA-Z\d]\d-?(?![sloibzSLOIBZ])[a-zA-Z](?![sloibzSLOIBZ])[a-zA-Z\d]\d-?(?![sloibzSLOIBZ])[a-zA-Z]{2}\d{2}\b");

    private bool IsInvalidAddress(SyncRecordDorothy record) => string.IsNullOrEmpty(record.StreetAddress1) ||
                                                               string.IsNullOrEmpty(record.City) ||
                                                               string.IsNullOrEmpty(record.State) ||
                                                               string.IsNullOrEmpty(record.ZipCode) ||
                                                               !Regex.IsMatch(record.ZipCode, @"^((\d{5})|([A-Z]\d[A-Z]\s\d[A-Z]\d))$");

    private bool IsInvalidName(SyncRecordDorothy record) => record.FirstName.Length > OpenPmConstants.NameMaxLength || record.LastName.Length > OpenPmConstants.NameMaxLength;

    private async Task<bool> IsShippableAddress(
        IMediator scopedMediator,
        IStatesService scopedStatesService,
        SyncRecordDorothy record)
    {
        var state = await scopedStatesService.GetByName(record.State);
        var command = new VerifyAddressCommand(
            streetAddress1: record.StreetAddress1,
            streetAddress2: record.StreetAddress2,
            city: record.City,
            stateAbbreviation: state.Abbreviation,
            zipCode: record.ZipCode);

        var result = await scopedMediator.Send(command);

        if (!result.ValidAddress || result.Deliverability == "undeliverable")
        {
            return false;
        }

        return true;
    }
    
    #endregion
}
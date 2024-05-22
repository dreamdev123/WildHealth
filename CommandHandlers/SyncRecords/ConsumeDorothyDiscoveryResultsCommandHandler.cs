using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Application.Utils.Spreadsheets;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.SyncRecords;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class ConsumeDorothyDiscoveryResultsCommandHandler : IRequestHandler<ConsumeDorothyDiscoveryResultsCommand>
{
    private SyncRecordStatus[] IgnoreStatuses = new SyncRecordStatus[]{ SyncRecordStatus.SyncComplete, SyncRecordStatus.ReadyForSync, SyncRecordStatus.Locked };
    private const string SyncDatumFirstName = "FirstName";
    private const string SyncDatumLastName = "LastName";
    private const string SyncDatumBirthday = "Birthday";
    private const string SyncRecordIdTitle = "syncrecordid";
    private const string FirstNameTitle = "f_name";
    private const string LastNameTitle = "l_name";
    private const string BirthdayTitle = "birthday";
    private const string PolicyIdTitle = "policy_id";
    private const string PolicyCarrierTitle = "policy_carrier";
    
    private readonly IMediator _mediator;
    private readonly ILogger<ConsumeDorothyDiscoveryResultsCommandHandler> _logger;
    private readonly ISyncRecordsService _syncRecordsService;

    public ConsumeDorothyDiscoveryResultsCommandHandler(
        IMediator mediator,
        ILogger<ConsumeDorothyDiscoveryResultsCommandHandler> logger,
        ISyncRecordsService syncRecordsService
        )
    {
        _syncRecordsService = syncRecordsService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(ConsumeDorothyDiscoveryResultsCommand command, CancellationToken cancellationToken)
    {
        var spreadsheetIterator = new SpreadsheetIterator(command.File);
        
        var importantTitles = new Dictionary<string, string>
        {
            { FirstNameTitle, string.Empty },
            { LastNameTitle, string.Empty },
            { BirthdayTitle, string.Empty },
            { PolicyIdTitle, string.Empty },
            { SyncRecordIdTitle, string.Empty },
            { PolicyCarrierTitle, string.Empty }
        };

        try
        {
            var results = new List<IDictionary<string, string>>();

            await spreadsheetIterator.Iterate(importantTitles, async (rowResults) =>
            {
                var firstName = rowResults[FirstNameTitle];
                var lastName = rowResults[LastNameTitle];
                var birthday = rowResults[BirthdayTitle];
                var policyId = rowResults[PolicyIdTitle];
                var syncRecordId = rowResults[SyncRecordIdTitle];
                var policyCarrier = rowResults[PolicyCarrierTitle];

                _logger.LogInformation(
                    $"Attempting to locate a sync record for [FirstName] = {firstName}, [LastName] = {lastName}, [Birthday] = {birthday}");
            
                var keys = new Dictionary<string, string>()
                {
                    {SyncDatumFirstName, firstName},
                    {SyncDatumLastName, lastName},
                    {SyncDatumBirthday, birthday}
                };

                SyncRecordDorothy[] syncRecords = new SyncRecordDorothy[]{};

                if (!string.IsNullOrEmpty(syncRecordId))
                {
                    syncRecords = await _syncRecordsService.GetByIdMultiple<SyncRecordDorothy>(id: Convert.ToInt32(syncRecordId));
                }

                if (!syncRecords.Any())
                {
                    syncRecords = await _syncRecordsService.GetByKeys<SyncRecordDorothy>(keys);
                }
                
                foreach (var syncRecord in syncRecords.Where(o => !IgnoreStatuses.Contains(o.SyncRecord.Status)))
                {
                    var id = syncRecord.GetId();

                    rowResults[SyncRecordIdTitle] = id.ToString();

                    results.Add(rowResults);
                }
            });
            
            foreach (var result in results.GroupBy(o => o[SyncRecordIdTitle]))
            {
                var syncRecordId = Convert.ToInt32(result.Key);
                var entries = result;

                var entry = entries.FirstOrDefault(o => IsPolicyCarrierMedicare(o[PolicyCarrierTitle])) ?? entries.FirstOrDefault();

                if (entry is not null)
                {
                    var policyCarrier = entry[PolicyCarrierTitle];
                    var policyId = entry[PolicyIdTitle];
                    
                    var syncRecord = await _syncRecordsService.GetById<SyncRecordDorothy>(syncRecordId);

                    syncRecord.PolicyId = IsPolicyIdValid(policyId) ? policyId : syncRecord.PolicyId;
                    syncRecord.PolicyCarrier = policyCarrier ?? string.Empty;
                
                    syncRecord.SyncRecord.Status = IsPolicyCarrierMedicare(syncRecord.PolicyCarrier) 
                        ? SyncRecordStatus.PendingCleansing 
                        : SyncRecordStatus.DorothyRequiresDiscoveryDiscarded;
                
                    await _syncRecordsService.UpdateAsync(syncRecord);
                }
                
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"Failed to locate sync record for update, {ex}");

            throw;
        }
    }
    
    private bool IsPolicyIdValid(string policyId)
    {
        return !string.IsNullOrEmpty(policyId);
    }


    private bool IsPolicyCarrierValid(string policyCarrier)
    {
        return !string.IsNullOrEmpty(policyCarrier);
    }

    private bool IsPolicyCarrierMedicare(string? policyCarrier)
    {
        return policyCarrier?.ToLower().StartsWith(OpenPmConstants.Organization.Medicare.ToLower()) ?? false;
    }
}
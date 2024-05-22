using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Enums.SyncRecords;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class CleanUpSyncRecordDorothyCommandHandler : IRequestHandler<CleanUpSyncRecordDorothyCommand>
{

    private readonly SyncRecordStatus[] _passedValidationStatuses = new[]
    {
        SyncRecordStatus.ReadyForSync, 
        SyncRecordStatus.SyncComplete, 
        SyncRecordStatus.FailedSync
    };
    
    private readonly SyncRecordStatus[] _discardedStatuses = new[]
    {
        SyncRecordStatus.DorothyInvalidBirthdayDiscarded, 
        SyncRecordStatus.DorothyRequiresDiscoveryDiscarded,
        SyncRecordStatus.DorothyBlankZipCodeDiscarded, 
        SyncRecordStatus.DorothyDuplicateBirthdateZipCodeDiscarded,
        SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded,
        SyncRecordStatus.DorothyInvalidAddressDiscarded,
        SyncRecordStatus.DorothyInvalidNameDiscarded,
        SyncRecordStatus.DorothyUnshippableAddressDiscarded
    };
    
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly ILogger<CleanUpSyncRecordDorothyCommandHandler> _logger;

    public CleanUpSyncRecordDorothyCommandHandler(
        ISyncRecordsService syncRecordsService,
        ILogger<CleanUpSyncRecordDorothyCommandHandler> logger)
    {
        _syncRecordsService = syncRecordsService;
        _logger = logger;
    }

    public async Task Handle(CleanUpSyncRecordDorothyCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleaning up invalid dorothy records: started");
        
        foreach (var status in command.StatusesToClean)
        {
            switch (status)
            {
                case SyncRecordStatus.DorothyDuplicatePolicyId:
                    await CleanUpDuplicatePolicyIds(command.NumberOfRecordsToClean);
                    break;
                default:
                    break;
            }
        }
        
        _logger.LogInformation("Cleaning up invalid dorothy records: finished");
    }

    #region private

    private async Task CleanUpDuplicatePolicyIds(int numberOfRecordsToClean)
    {

        var ignoreStatuses = _discardedStatuses.Concat(new SyncRecordStatus[]
        {
            SyncRecordStatus.DorothyRequiresDiscovery
        }).ToArray();    
        
        for (var j = 0; j < numberOfRecordsToClean; j++)
        {
            var recordsToClean = await _syncRecordsService.GetByTypeAndStatus<SyncRecordDorothy>(
                statuses: new[]
                {
                    SyncRecordStatus.DorothyDuplicatePolicyId
                },
                type: SyncRecordType.Dorothy,
                count: 1);
            
            // Run them all through a series of validations to determine which one is most credible
            // If anyone doesn't have a reasonable birthday, then move it to discarded
            // If anyone doesn't have a zip code, move it to discarded
            // If any is non-blank between (firstname, lastname, email, phone, gender) then remove it from contention
            
            // any remaining at this point, just grab one and move other to discarded

            // want to go with most recently added entries
            foreach (var record in recordsToClean.ToArray())
            {
                var keys = new Dictionary<string, string>()
                {
                    {nameof(SyncRecordDorothy.PolicyId), record.PolicyId!}
                };

                // Only concerned with statuses that are not discarded
                var allRecords = (await _syncRecordsService.GetByKeys<SyncRecordDorothy>(
                        keys: keys, 
                        ignoreStatuses: ignoreStatuses))
                    .ToArray();

                var policyId = record.PolicyId;

                /////////////////////////////////////////////////////////////////////////////////////////////////
                // If the policyId that's duplicated is completely wrong, we want to move to requires discovery
                // for 1 record in each group and send the others to discarded
                /////////////////////////////////////////////////////////////////////////////////////////////////
                if (IsGenericPolicyId(policyId!))
                {
                    foreach (var grouping in allRecords.GroupBy(o => new { o.FirstName, o.LastName }))
                    {
                        for (var i = 0; i < grouping.Count(); i++)
                        {
                            var dorothyRecord = grouping.ElementAt(i);

                            if (i == 0)
                            {
                                await SetDorothyRecordToStatus(dorothyRecord, SyncRecordStatus.DorothyRequiresDiscovery);

                                continue;
                            }

                            await SetDorothyRecordToStatus(dorothyRecord, SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded);
                        }
                    }

                    continue;
                }

                ////////////////////////////////////////////////////////////////////////
                // If a record is already validated they handle the others
                ////////////////////////////////////////////////////////////////////////
                var alreadyValidatedRecords =
                    allRecords.Where(o => _passedValidationStatuses.Contains(o.SyncRecord.Status)).ToArray();

                if (alreadyValidatedRecords.Any())
                {
                    foreach (var nonValidatedRecord in allRecords.Except(alreadyValidatedRecords))
                    {
                        await SetDorothyRecordToStatus(nonValidatedRecord,
                            SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded);
                    }

                    continue;
                }

                ////////////////////////////////////////////////////////////////////////
                // This means they all seem to be legit, we need to grab one based
                // on some criteria and send the others to discarded, do this by
                // sending items to discarded based on criteria, if there are some
                // left at the end then just grab one and send others to discarded
                ////////////////////////////////////////////////////////////////////////
                var passedDobRecords = new List<SyncRecordDorothy>();

                ////////////////////////////////////////////////////////////////////////
                // Consider DOB, if it cannot be parsed or too young then remove it
                ////////////////////////////////////////////////////////////////////////
                foreach (var dorothyRecord in allRecords)
                {
                    try
                    {
                        var birthday = DateTime.Parse(dorothyRecord.Birthday);

                        if (birthday > DateTime.UtcNow.AddYears(-18))
                        {
                            await SetDorothyRecordToStatus(dorothyRecord,
                                SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded);

                            continue;
                        }

                        passedDobRecords.Add(dorothyRecord);
                    }
                    catch (Exception)
                    {
                        await SetDorothyRecordToStatus(dorothyRecord,
                            SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded);
                    }
                }

                ////////////////////////////////////////////////////////////////////////
                // Consider ZipCode, if it's blank then move to discarded
                ////////////////////////////////////////////////////////////////////////
                var passedZipCodeRecords = new List<SyncRecordDorothy>();

                foreach (var passedDobRecord in passedDobRecords)
                {
                    if (string.IsNullOrEmpty(passedDobRecord.ZipCode))
                    {
                        await SetDorothyRecordToStatus(passedDobRecord,
                            SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded);

                        continue;
                    }

                    passedZipCodeRecords.Add(passedDobRecord);
                }


                ////////////////////////////////////////////////////////////////////////
                // Consider fields to make sure they are not blank
                ////////////////////////////////////////////////////////////////////////
                var passedNonBlankRecords = new List<SyncRecordDorothy>();

                // only want to run this validation if there's at least one that has 
                // non-blank fields - it's not a deal breaker
                if (!passedZipCodeRecords.All(o => HasBlankFields(o)))
                {
                    foreach (var passedZipCodeRecord in passedZipCodeRecords)
                    {
                        if (HasBlankFields(passedZipCodeRecord))
                        {
                            await SetDorothyRecordToStatus(passedZipCodeRecord,
                                SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded);

                            continue;
                        }

                        passedNonBlankRecords.Add(passedZipCodeRecord);
                    }
                }
                else
                {
                    passedNonBlankRecords = passedZipCodeRecords;
                }

                // If there was only a single record in this state and it fails everything, move it to discarded
                if (allRecords.Length == 1 && HasBlankFields(allRecords.First()))
                {
                    await SetDorothyRecordToStatus(allRecords.First(),
                        SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded);

                    continue;
                }
                    

                ////////////////////////////////////////////////////////////////////////
                // All of these have passed preliminary validations, want to just grab
                // one and send others to discarded
                ////////////////////////////////////////////////////////////////////////
                for (var i = 0; i < passedNonBlankRecords.Count(); i++)
                {
                    var dorothyRecord = passedNonBlankRecords[i];

                    if (i == 0)
                    {
                        await SetDorothyRecordToStatus(dorothyRecord,
                            SyncRecordStatus.PendingValidation);

                        continue;
                    }

                    await SetDorothyRecordToStatus(dorothyRecord,
                        SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded);
                }
            }
        }
    }

    private SyncRecordStatus GetDiscardedVariant(SyncRecordStatus status)
    {
        switch (status)
        {
            case SyncRecordStatus.DorothyDuplicatePolicyId:
                return SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded;
            default:
                throw new Exception($"Unable to find discarded variant for {status.ToString()}");
        }
    }
    
    private async Task SetDorothyRecordToStatus(SyncRecordDorothy record, SyncRecordStatus status)
    {
        var realRecord = await _syncRecordsService.GetById(record.SyncRecord.GetId());
        realRecord.Status = status;

        await _syncRecordsService.UpdateAsync(realRecord);
    }

    private bool HasBlankFields(SyncRecordDorothy record) => string.IsNullOrEmpty(record.FirstName) ||
                                                             string.IsNullOrEmpty(record.LastName) ||
                                                             string.IsNullOrEmpty(record.Gender) ||
                                                             string.IsNullOrEmpty(record.Email) ||
                                                             string.IsNullOrEmpty(record.PhoneNumber);
    private bool IsGenericPolicyId(string source) => IsAllLetters(source) || IsAllNumberrs(source);
    private bool IsAllNumberrs(string source) => Regex.IsMatch(source,@"^[0-9]*$");
    private bool IsAllLetters(string source) => Regex.IsMatch(source,@"^[A-Za-z]+$");


    #endregion
}
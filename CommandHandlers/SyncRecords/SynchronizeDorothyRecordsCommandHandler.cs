#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.Insurances;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.States;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.SyncRecords;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.SyncRecords;
using WildHealth.Domain.Enums.User;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Services;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class SynchronizeDorothyRecordsCommandHandler : IRequestHandler<SynchronizeDorothyRecordsCommand>
{
    private readonly IServiceProvider _services;
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly ILogger<SynchronizeDorothyRecordsCommandHandler> _logger;

    public SynchronizeDorothyRecordsCommandHandler(
        IServiceProvider services,
        ILogger<SynchronizeDorothyRecordsCommandHandler> logger)
    {
        _services = services;
        _logger = logger;
        
        var scope = _services.CreateScope();
        _syncRecordsService = scope.ServiceProvider.GetRequiredService<ISyncRecordsService>();
    }

    public async Task Handle(SynchronizeDorothyRecordsCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Synchronizing of Dorothy records into OpenPM: started");

        var statusesToSync = new[] { SyncRecordStatus.ReadyForSync };
        var recordsToSync = await _syncRecordsService.GetByTypeAndStatus<SyncRecordDorothy>(
            type: SyncRecordType.Dorothy,
            statuses: statusesToSync,
            count: command.NumberOfRecordsToSynchronize,
            practiceId: command.PracticeId);

        var numberOfThreads = (command.NumberOfRecordsToSynchronize / command.ShardSize) + 1;

        var shards = recordsToSync.Split(numberOfThreads).ToArray();

        await Parallel.ForEachAsync(shards, async (shard, token) => { await Synchronize(shard.ToArray(), command.SkipSync, token); });
    }


    #region private

    private async Task Synchronize(SyncRecordDorothy[] recordsToSync, bool skipSync, CancellationToken cancellationToken)
    {
        var scope = _services.CreateScope();
        var scopedSyncRecordsService = scope.ServiceProvider.GetRequiredService<ISyncRecordsService>();
        var scopedLocationsService = scope.ServiceProvider.GetRequiredService<ILocationsService>();
        var scopedIntegrationsService = scope.ServiceProvider.GetRequiredService<IIntegrationsService>();
        var scopedInsurancesService = scope.ServiceProvider.GetRequiredService<IInsuranceService>();
        var scopedPracticeManagementIntegrationService = scope.ServiceProvider.GetRequiredService<IPracticeManagementIntegrationServiceFactory>();
        var scopedStatesService = scope.ServiceProvider.GetRequiredService<IStatesService>();
        
        ////////////////////////////////////////////////////////////////////////
        // First update all statuses to locked
        ////////////////////////////////////////////////////////////////////////
        try
        {
            foreach (var recordToSync in recordsToSync)
            {
                await UpdateRecordStatus(recordToSync.SyncRecord, SyncRecordStatus.Locked, scopedSyncRecordsService);
            }
        }
        catch (Exception)
        {
            _logger.LogError($"Unable to set all statuses to locked");
            throw;
        }

        if (skipSync)
        {
            foreach (var record in recordsToSync)
            {
                await UpdateRecordStatus(record.SyncRecord, SyncRecordStatus.SyncComplete, scopedSyncRecordsService);
            }

            return;
        }
        
        ////////////////////////////////////////////////////////////////////////
        // Run synchronization with OpenPM
        ////////////////////////////////////////////////////////////////////////
        foreach (var record in recordsToSync)
        {
            try
            {
                var scopedPmService =
                    await scopedPracticeManagementIntegrationService.CreateAsync(record.SyncRecord.PracticeId);
                
                var fhirIntegrationId = record.SyncRecord.GetIntegrationId(IntegrationVendor.OpenPm);
                if (string.IsNullOrEmpty(fhirIntegrationId))
                {
                    await GetOrCreateFhirPatient(
                        record,
                        scopedPmService,
                        scopedLocationsService,
                        scopedStatesService,
                        scopedIntegrationsService,
                        scopedInsurancesService);
                }

                await UpdateRecordStatus(record.SyncRecord, SyncRecordStatus.SyncComplete, scopedSyncRecordsService);
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Failed to sync dorothy record with Id = {record.SyncRecord.Id}: {e}");
                
                await UpdateRecordStatus(record.SyncRecord, SyncRecordStatus.FailedSync, scopedSyncRecordsService);
            }
        }
    }

    private async Task<string> GetOrCreateFhirPatient(
        SyncRecordDorothy record,
        IPracticeManagementIntegrationService scopedPmService,
        ILocationsService scopedLocationsService,
        IStatesService scopedStatesService,
        IIntegrationsService scopedIntegrationsService,
        IInsuranceService scopedInsurancesService)
    {
        var existingPatientId = await GetFhirPatient(record, scopedPmService, scopedLocationsService);

        if (!string.IsNullOrEmpty(existingPatientId))
        {
            await CreateIntegration(record.SyncRecord, existingPatientId, scopedIntegrationsService);
            
            return existingPatientId;
        }

        var newPatientId = await CreateFhirPatient(
            record, 
            scopedPmService,
            scopedLocationsService,
            scopedStatesService,
            scopedIntegrationsService,
            scopedInsurancesService);

        return newPatientId;
    }

    private async Task<string?> GetFhirPatient(
        SyncRecordDorothy record, 
        IPracticeManagementIntegrationService scopedPmService, 
        ILocationsService scopedLocationsService)
    {
        var birthday = DateTime.Parse(record.Birthday);
        
        var filter = new Dictionary<string, object>()
        {
            { "given", record.FirstName },
            { "family", record.LastName },
            { "birthdate", birthday.ToString("yyyy-MM-dd") }
        };

        var fhirPatients = await scopedPmService.QueryPatientsAsync(record.SyncRecord.PracticeId, filter);
        
        /* ------- Disabled facility matching for initial upload -------*/
        // var location = await scopedLocationsService.GetDefaultLocationAsync(record.SyncRecord.PracticeId);
        // var fhirLocationId = location.GetIntegrationId(IntegrationVendor.OpenPm, IntegrationPurposes.Location.DorothyId);

        var patient = fhirPatients.FirstOrDefault();
        
        return patient?.Id;
    }

    private async Task<string> CreateFhirPatient(
        SyncRecordDorothy record,
        IPracticeManagementIntegrationService scopedPmService,
        ILocationsService scopedLocationsService,
        IStatesService scopedStatesService,
        IIntegrationsService scopedIntegrationsService,
        IInsuranceService scopedInsurancesService)
    {
        var location = await scopedLocationsService.GetDefaultLocationAsync(record.SyncRecord.PracticeId);
        var fhirLocationId =
            location.GetIntegrationId(IntegrationVendor.OpenPm, IntegrationPurposes.Location.DorothyId);

        var state = await scopedStatesService.GetByName(record.State);

        var fhirPatientId = await scopedPmService.CreatePatientAsync(
            firstName: record.FirstName,
            lastName: record.LastName,
            gender: GetGender(record.Gender),
            birthday: DateTime.Parse(record.Birthday),
            phoneNumber: record.PhoneNumber,
            email: record.Email,
            streetAddress1: record.StreetAddress1,
            streetAddress2: record.StreetAddress2,
            city: record.City,
            state: state.Abbreviation,
            zipCode: record.ZipCode,
            fhirLocationId: fhirLocationId,
            practiceId: record.SyncRecord.PracticeId,
            fhirProviderId: OpenPmConstants.Practitioner.DorothyAssignedProvider
        );

        await CreateIntegration(
            record.SyncRecord, 
            fhirPatientId,
            scopedIntegrationsService);

        await CreateFhirGuarantorAsync(
            record,
            fhirPatientId,
            state.Abbreviation,
            scopedPmService);

        var fhirCoverageId = await CreateFhirCoverageAsync(
            record, 
            fhirPatientId,
            scopedPmService,
            scopedInsurancesService);

        await CreateFhirAccountAsync(
            fhirPatientId, 
            fhirCoverageId, 
            record,
            scopedPmService);

        return fhirPatientId;
    }

    private async Task<string> CreateFhirCoverageAsync(
        SyncRecordDorothy record, 
        string fhirPatientId,
        IPracticeManagementIntegrationService scopedPmService,
        IInsuranceService scopedInsurancesService)
    {
        var insurance = await scopedInsurancesService.GetByNameAsync(OpenPmConstants.Organization.Medicare);
        var fhirInsuranceId = insurance.GetIntegrationId(IntegrationVendor.OpenPm);
        
        var coverageId = await scopedPmService.CreateCoverageAsync(
            patientId: fhirPatientId,
            memberId: record.PolicyId,
            policyHolderId: string.Empty,
            policyHolderRelationshipCode: OpenPmConstants.RelatedPersons.SelfRelation,
            organizationId: fhirInsuranceId,
            practiceId: record.SyncRecord.PracticeId
        );

        return coverageId;
    }
    
    private async Task<string> CreateFhirGuarantorAsync(
        SyncRecordDorothy record,
        string fhirPatientId,
        string stateAbbreviation,
        IPracticeManagementIntegrationService scopedPmService)
    {
        var policyHolderId = await scopedPmService.CreateGuarantorAsync(
            firstName: record.FirstName,
            lastName: record.LastName,
            birthday: DateTime.Parse(record.Birthday),
            gender: GetGender(record.Gender),
            streetAddress1: record.StreetAddress1,
            streetAddress2: record.StreetAddress2,
            city: record.City,
            state: stateAbbreviation,
            zipCode: record.ZipCode,
            patientId: fhirPatientId,
            practiceId: record.SyncRecord.PracticeId,
            relationship: OpenPmConstants.RelatedPersons.SelfRelation
        );
            
        return policyHolderId;
    }
    
    private async Task CreateFhirAccountAsync(
        string patientId, 
        string coverageId, 
        SyncRecordDorothy record,
        IPracticeManagementIntegrationService scopedPmService)
    {
        await scopedPmService.CreateAccountAsync(
            patientId: patientId,
            fullName: record.GetFullname(),
            coverageIds: new[] { coverageId },
            practiceId: record.SyncRecord.PracticeId
        );
    }

    private async Task CreateIntegration(
        SyncRecord record, 
        string fhirPatientId,
        IIntegrationsService scopedIntegrationsService)
    {
        var integration = new SyncRecordIntegration(
            IntegrationVendor.OpenPm,
            IntegrationPurposes.SyncRecord.ExternalId,
            fhirPatientId,
            record);

        await scopedIntegrationsService.CreateAsync(integration);
    }

    private async Task UpdateRecordStatus(SyncRecord record, SyncRecordStatus status, ISyncRecordsService scopedSyncRecordService)
    {
        record.Status = status;
        await scopedSyncRecordService.UpdateAsync(record);
    }

    private Gender GetGender(string gender)
    {
        switch (gender)
        {
            case "Male":
                return Gender.Male;
            case "Female":
                return Gender.Female;
            default:
                return Gender.None;
        }
    }

    #endregion
}
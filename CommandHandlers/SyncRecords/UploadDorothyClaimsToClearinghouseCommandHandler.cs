using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.Insurances;
using WildHealth.Application.Services.States;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Models.Insurance;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class UploadDorothyClaimsToClearinghouseCommandHandler : IRequestHandler<UploadDorothyClaimsToClearinghouseCommand>
{
    private const int UnitCost = 12;
    private const int DefaultTestQuantity = 8;
    
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IClaimsService _claimsService;
    private readonly IStatesService _statesService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UploadDorothyClaimsToClearinghouseCommandHandler> _logger;

    public UploadDorothyClaimsToClearinghouseCommandHandler(
        ISyncRecordsService syncRecordsService,
        IClaimsService claimsService,
        IStatesService statesService,
        IMediator mediator,
        IMapper mapper,
        IEventBus eventBus,
        ILogger<UploadDorothyClaimsToClearinghouseCommandHandler> logger)
    {
        _syncRecordsService = syncRecordsService;
        _claimsService = claimsService;
        _statesService = statesService;
        _eventBus = eventBus;
        _mediator = mediator;
        _mapper = mapper;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(UploadDorothyClaimsToClearinghouseCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Upload of eligible dorothy claims for practice id = {command.PracticeId} to clearinghouse has: started");

        var billableDate = DateTime.UtcNow.AddDays(-31);

        var records = await _syncRecordsService.GetBillableDorothyRecords(
            billableDate: billableDate,
            practiceId: command.PracticeId);

        if (records.IsNullOrEmpty())
        {
            _logger.LogInformation($"Unable to find any eligible dorothy patients for practice id = {command.PracticeId} to bill.");
            return;
        }

        var claims = await CreateAndUploadClaims(records, command.PracticeId);
        
        foreach (var claim in claims)
        {
            try
            {
                await _eventBus.Publish(new FhirClaimIntegrationEvent(
                    payload: new FhirClaimStatusChangedPayload(
                        id: claim.GetId(),
                        practiceId: command.PracticeId,
                        newStatusId: Convert.ToInt32(WildHealth.Domain.Enums.Insurance.ClaimStatus.Submitted),
                        entity: String.Empty,
                        category: String.Empty,
                        statusCode: String.Empty,
                        date: DateTime.UtcNow
                    ),
                    user: new UserMetadataModel(claim.ClaimantUniversalId.ToString()),
                    eventDate: DateTime.UtcNow
                ), cancellationToken);
                
                await PublishClaimSubmittedIntegrationEvent(claim, command.PracticeId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to update claim status for claim id = {claim.GetId()} {ex}");
            }
        }
        
        _logger.LogInformation($"Upload of eligible dorothy claims for practice id = {command.PracticeId} to clearinghouse has: finished");
    }

    #region private

    private async Task<Claim[]> CreateAndUploadClaims(SyncRecordDorothy[] records, int practiceId)
    {
        var professionalClaims = Enumerable.Empty<ProfessionalClaimModel>();
        var claims = Enumerable.Empty<Claim>();

        var serviceFromDate = DateTime.UtcNow;
        
        foreach (var syncRecordDorothy in records)
        {
            var state = await _statesService.GetByName(syncRecordDorothy.State);
            
            var patientAddress = syncRecordDorothy.StreetAddress2 is not null
                ? $"{syncRecordDorothy.StreetAddress1} {syncRecordDorothy.StreetAddress2}"
                : syncRecordDorothy.StreetAddress1;

            var testQuantity = syncRecordDorothy.TestQuantity <= 0 ? DefaultTestQuantity : syncRecordDorothy.TestQuantity;
            var chargeAmount = testQuantity * UnitCost;
            
            var claim = new Claim
            {
                ClaimantUniversalId = syncRecordDorothy.SyncRecord.UniversalId,
                ClaimantSyncRecord = syncRecordDorothy.SyncRecord,
                ClaimStatus = WildHealth.Domain.Enums.Insurance.ClaimStatus.Created,
                SubscriberId = syncRecordDorothy.PolicyId,
                PatientLastName = syncRecordDorothy.LastName.ToUpper(),
                PatientFirstName = syncRecordDorothy.FirstName.ToUpper(),
                PatientBirthday = DateTime.Parse(syncRecordDorothy.Birthday),
                PatientSex = syncRecordDorothy.Gender.First().ToString().ToUpper(),
                SubscriberLastName = syncRecordDorothy.LastName.ToUpper(),
                SubscriberFirstName = syncRecordDorothy.FirstName.ToUpper(),
                PatientAddress = patientAddress,
                PatientCity = syncRecordDorothy.City.ToUpper(),
                PatientState = state.Abbreviation.ToUpper(),
                PatientZip = syncRecordDorothy.ZipCode,
                PatientRelationship = DorothyConstants.Claims.MurrayMedical.PatientRelationship,
                SubscriberAddress = patientAddress,
                SubscriberCity = syncRecordDorothy.City.ToUpper(),
                SubscriberState = state.Abbreviation.ToUpper(),
                SubscriberZip = syncRecordDorothy.ZipCode,
                SubscriberBirthday = DateTime.Parse(syncRecordDorothy.Birthday),
                SubscriberSex = syncRecordDorothy.Gender.First().ToString().ToUpper(),
                DiagnosisCodeA = DorothyConstants.Claims.MurrayMedical.DiagnosisCode,
                IcdIndicator = DorothyConstants.Claims.MurrayMedical.IcdIndicator,
                BillingTaxId = DorothyConstants.Claims.MurrayMedical.BillingTaxId,
                BillingTaxIdType = DorothyConstants.Claims.MurrayMedical.BillingTaxIdType,
                BillingName = DorothyConstants.Claims.MurrayMedical.BillingName,
                BillingAddress1 = DorothyConstants.Claims.MurrayMedical.BillingAddress1,
                BillingCity = DorothyConstants.Claims.MurrayMedical.BillingCity,
                BillingState = DorothyConstants.Claims.MurrayMedical.BillingState,
                BillingZip = DorothyConstants.Claims.MurrayMedical.BillingZip,
                BillingNpi = DorothyConstants.Claims.MurrayMedical.BillingNpi,
                PayerName = DorothyConstants.Claims.MurrayMedical.PayerName,
                Procedure = new ClaimProcedure(
                    serviceFrom: serviceFromDate,
                    placeOfService: DorothyConstants.Claims.MurrayMedical.PlaceOfService,
                    procedureCode: DorothyConstants.Claims.MurrayMedical.ProcedureCode,
                    diagnosisPointers: DorothyConstants.Claims.MurrayMedical.DiagnosisPointers,
                    chargeAmount: chargeAmount.ToString("F2"),
                    units: testQuantity,
                    renderingProviderNpi: DorothyConstants.Claims.MurrayMedical.RenderingProviderNpi),
                ReferringProviderFirstName = DorothyConstants.Claims.MurrayMedical.ReferringProviderFirstName,
                ReferringProviderLastName = DorothyConstants.Claims.MurrayMedical.ReferringProviderLastName,
                ReferringProviderNpi = DorothyConstants.Claims.MurrayMedical.ReferringProviderNpi,
                ReferringProviderQualifier = DorothyConstants.Claims.MurrayMedical.ReferringProviderQualifier,
                RenderingProviderFirstName = DorothyConstants.Claims.MurrayMedical.RenderingProviderFirstName,
                RenderingProviderLastName = DorothyConstants.Claims.MurrayMedical.RenderingProviderLastName,
                RenderingProviderSignatureDate = syncRecordDorothy.SyncRecord.CreatedAt
            };

            claim = await _claimsService.CreateAsync(claim);
            var professionalClaim = _mapper.Map<ProfessionalClaimModel>(claim);

            professionalClaims = professionalClaims.Append(professionalClaim);
            claims = claims.Append(claim);
        }
        
        var uploadClaimsCommand = new UploadClaimsToClearinghouseCommand(
            claims: professionalClaims.ToArray(),
            practiceId: practiceId);
        
        await _mediator.Send(uploadClaimsCommand);

        return claims.ToArray();
    }

    private async Task PublishClaimSubmittedIntegrationEvent(Claim claim, int practiceId)
    {
        var payload = new FhirClaimSubmittedPayload(
            id: claim.GetId(),
            practiceId: practiceId, 
            date: DateTime.UtcNow);

        await _eventBus.Publish(new FhirClaimIntegrationEvent(
            payload: payload,
            user: new UserMetadataModel(universalId: claim.ClaimantUniversalId.ToString()),
            eventDate: DateTime.UtcNow));
    }

    #endregion
}
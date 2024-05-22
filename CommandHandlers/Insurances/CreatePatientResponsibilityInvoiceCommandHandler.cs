using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Insurances;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Notes;
using WildHealth.Application.Services.ProductsPayment;
using WildHealth.Application.Services.States;
using WildHealth.Common.Constants;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Insurance;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.Invoices;
using WildHealth.Settings;
using WildHealth.Shared.Exceptions;
using ClaimStatus = WildHealth.Domain.Enums.Insurance.ClaimStatus;
using IntegrationVendor = WildHealth.Domain.Enums.Integrations.IntegrationVendor;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class CreatePatientResponsibilityInvoiceCommandHandler : IRequestHandler<CreatePatientResponsibilityInvoiceCommand>
{
    private readonly INoteService _noteService;
    private readonly IIntegrationsService _integrationsService;
    private readonly IProductsPaymentService _productsPaymentService;
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
    private readonly IClaimsService _claimsService;
    private readonly IStatesService _statesService;
    private readonly ISettingsManager _settingsManager;
    private readonly IMediator _mediator;
    private readonly ILogger<CreatePatientResponsibilityInvoiceCommandHandler> _logger;

    public CreatePatientResponsibilityInvoiceCommandHandler(
        INoteService noteService,
        IIntegrationsService integrationsService,
        IProductsPaymentService productsPaymentService,
        IIntegrationServiceFactory integrationServiceFactory,
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory,
        IClaimsService claimsService,
        IStatesService statesService,
        ISettingsManager settingsManager,
        IMediator mediator,
        ILogger<CreatePatientResponsibilityInvoiceCommandHandler> logger)
    {
        _noteService = noteService;
        _integrationsService = integrationsService;
        _productsPaymentService = productsPaymentService;
        _integrationServiceFactory = integrationServiceFactory;
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        _claimsService = claimsService;
        _statesService = statesService;
        _settingsManager = settingsManager;
        _mediator = mediator;
        _logger = logger;
    }
    
    public async Task Handle(CreatePatientResponsibilityInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Creating patient responsibility invoice for note id = {command.NoteId} has: started");
        
        var note = await _noteService.GetByIdAsync(command.NoteId);

        var user = note.Patient.User;

        var claim = note.Claims.FirstOrDefault() ?? await CreateClaim(note);
        
        claim = AssertIsValidClaim(claim);

        var stateAbbreviation = await GetStateAbbreviation(user);
        
        var integrationService = await _integrationServiceFactory.CreateAsync(user.PracticeId);

        try
        {
            var invoice = await CreateInvoice(
                copayAmount: command.CopayAmount,
                coinsuranceAmount: command.CoinsuranceAmount,
                deductibleAmount: command.DeductibleAmount,
                stateAbbreviation: stateAbbreviation,
                patient: note.Patient,
                appointment: note.Appointment,
                practiceId: user.PracticeId);

            await CreateClaimIntegration(claim, invoice.Id, integrationService.IntegrationVendor);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Creating patient responsibility invoice for note id = {command.NoteId} has: failed {ex}");

            throw;
        }
    }

    #region private

     private Claim AssertIsValidClaim(Claim? claim)
     {
         if (claim is null)
         {
             throw new AppException(HttpStatusCode.BadRequest, $"Could not create or find claim to create an invoice");
         }
         
         var invoiceId = claim.GetIntegrationId(IntegrationVendor.Stripe);
         
        if (invoiceId is not null)
        {
            throw new AppException(HttpStatusCode.BadRequest, $"Invoice already exist for claim id = {claim.GetId()}");
        }

        return claim;
     }

     private async Task<string> GetStateAbbreviation(User user)
     {
         return user.BillingAddress.State.Length == 2
             ? user.BillingAddress.State
             : (await _statesService.GetByName(user.BillingAddress.State)).Abbreviation;
     }


    private async Task<InvoiceIntegrationModel> CreateInvoice(
        decimal copayAmount,
        decimal coinsuranceAmount,
        decimal deductibleAmount,
        string stateAbbreviation,
        Patient patient,
        Appointment appointment,
        int practiceId)
    {
        string[] settingNames =
        {
            SettingsNames.Stripe.CoinsuranceProductId,
            SettingsNames.Stripe.CopayProductId,
            SettingsNames.Stripe.DeductibleProductId,
            SettingsNames.Stripe.AccountTaxId(stateAbbreviation)
        };
        var settings = await _settingsManager.GetSettings(settingNames, practiceId);
        
        var items = new (string productId, decimal price)[]
        {
            (settings[SettingsNames.Stripe.CopayProductId], copayAmount),
            (settings[SettingsNames.Stripe.CoinsuranceProductId], coinsuranceAmount),
            (settings[SettingsNames.Stripe.DeductibleProductId], deductibleAmount)
        };

        var memo =
            $"Patient responsibility invoice for your appointment on {appointment.StartDate.Date.ToShortDateString()}. If you have any questions, please reach out to your insurance provider for your explanation of benefits (EOB).";
        
        return await _productsPaymentService.ProcessProductPatientResponsibilityPaymentAsync(
            patient: patient,
            items: items,
            accountTaxId: settings[SettingsNames.Stripe.AccountTaxId(stateAbbreviation)],
            memo: memo);
    }

    private async Task CreateClaimIntegration(
        Claim claim, 
        string value, 
        IntegrationVendor vendor)
    {
        var claimIntegration = new ClaimIntegration(
            vendor: vendor,
            purpose: IntegrationPurposes.Claim.ExternalId, 
            value: value,
            claim: claim);

        await _integrationsService.CreateAsync(claimIntegration);
    }

    private async Task<Claim?> CreateClaim(Note note)
    {
        var user = note.Patient.User;

        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(user.PracticeId);

        var pmClaim = await _mediator.Send(new GetNoteClaimCommand(note.GetId()));

        if (pmClaim is null)
        {
            return null;
        }
        
        var coverage = await _mediator.Send(new GetCoveragesCommand(userId: user.GetId()));

        DateTime.TryParse(pmClaim.Items.FirstOrDefault()?.ServicedPeriod.Start, out var serviceDate);
        
        var claim = await _claimsService.CreateAsync(new Claim
        {
            ClaimantUniversalId = note.UniversalId,
            ClaimantNote = note,
            ClaimStatus = ClaimStatus.Paid,
            SubscriberId = coverage.FirstOrDefault(o => o.Status == CoverageStatus.Active)?.MemberId ?? string.Empty,
            PatientLastName = user.LastName.ToUpper(),
            PatientFirstName = user.FirstName.ToUpper(),
            PatientBirthday = user.Birthday ?? DateTime.Now,
            PatientAddress = $"{user.BillingAddress.StreetAddress1} {user.BillingAddress.StreetAddress2}",
            PatientCity = user.BillingAddress.City.ToUpper(),
            PatientState = user.BillingAddress.State.ToUpper(),
            PatientZip = user.BillingAddress.ZipCode,
            DiagnosisCodeA = pmClaim.Diagnoses.FirstOrDefault()?.Diagnosis.Codings.FirstOrDefault()?.Code,
            Procedure = new ClaimProcedure(
                serviceFrom: serviceDate,
                placeOfService: string.Empty,
                procedureCode: string.Empty,
                diagnosisPointers: string.Empty,
                chargeAmount: pmClaim.Items.FirstOrDefault()?.UnitPrice.Value.ToString() ?? string.Empty,
                units: Convert.ToInt32(pmClaim.Items.FirstOrDefault()?.Quantity.Value),
                renderingProviderNpi: string.Empty)
        });
        
        await CreateClaimIntegration(claim, pmClaim.Id, pmService.Vendor);

        return claim;
    }

    #endregion
}
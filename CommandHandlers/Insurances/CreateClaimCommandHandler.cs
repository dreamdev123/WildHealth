using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Coverages;
using WildHealth.Application.Services.InsuranceConfigurations;
using WildHealth.Application.Services.Insurances;
using WildHealth.Application.Services.MdmCodes;
using WildHealth.Application.Services.Notes;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.States;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.InsuranceConfigurations;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.MdmCodes;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Insurance;
using WildHealth.Domain.Enums.InsuranceConfigurations;
using WildHealth.Domain.Models.Insurances;
using WildHealth.Shared.Exceptions;
using ClaimStatus = WildHealth.Domain.Enums.Insurance.ClaimStatus;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class CreateClaimCommandHandler : IRequestHandler<CreateClaimCommand, Claim?>
{
    private readonly INoteService _noteService;
    private readonly IClaimsService _claimsService;
    private readonly ICoveragesService _coveragesService;
    private readonly IInsuranceConfigsService _insuranceConfigsService;
    private readonly IMdmCodesService _mdmCodesService;
    private readonly IStatesService _statesService;
    private readonly IPracticeEntityService _practiceEntityService;
    private readonly ILogger<CreateClaimCommandHandler> _logger;
    private readonly IMediator _mediator;

    public CreateClaimCommandHandler(
        INoteService noteService,
        IClaimsService claimsService,
        ICoveragesService coveragesService,
        IInsuranceConfigsService insuranceConfigsService,
        IMdmCodesService mdmCodesService,
        IStatesService statesService,
        IPracticeEntityService practiceEntityService,
        ILogger<CreateClaimCommandHandler> logger,
        IMediator mediator)
    {
        _noteService = noteService;
        _claimsService = claimsService;
        _coveragesService = coveragesService;
        _insuranceConfigsService = insuranceConfigsService;
        _mdmCodesService = mdmCodesService;
        _statesService = statesService;
        _practiceEntityService = practiceEntityService;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<Claim?> Handle(CreateClaimCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Creating claim for note id = {command.NoteId} has: started");
        
        var note = await _noteService.GetByIdAsync(command.NoteId);

        if (!note.CompletedAt.HasValue)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Can't create claim for incomplete note with note id = {note.GetId()}");
        }

        var user = note.Patient.User;
        
        var employee = note.Employee;

        var appointment = note.Appointment;

        var coverage = await GetCoverage(userId: user.GetId());
        
        var serviceConfig = await GetServiceConfig(
            practiceId: user.PracticeId, 
            insuranceId: coverage.InsuranceId,
            appointment: appointment);

        if (!serviceConfig.SupportsClaims)
        {
            _logger.LogInformation($"Creating claim for note id = {command.NoteId} has: stopped due to claims not being supported for insurance id = {coverage.InsuranceId}");

            return null;
        }

        var noteContent = GetNoteContent(note: note);
        
        var mdmCode = await _mdmCodesService.GetByIdAsync(noteContent.Mdm.SelectedCodeId);
        
        var state = user.BillingAddress.State.Length == 2 
            ? await _statesService.GetByAbbreviation(user.BillingAddress.State)
            : await _statesService.GetByName(user.BillingAddress.State);

        var practiceEntity = await _practiceEntityService.GetByPracticeAndStateAsync(
            practiceId: user.PracticeId, 
            stateId: state.GetId());
        
        var claim = await CreateClaim(
            user: user, 
            employee: employee, 
            coverage: coverage, 
            note: note,
            noteContent: noteContent,
            mdmCode: mdmCode, 
            serviceConfig: serviceConfig,
            practiceEntity: practiceEntity,
            claimStatus: command.ClaimStatus);
            
        _logger.LogInformation($"Creating claim for note id = {command.NoteId} has: finished");

        return claim;
    }

    #region private

    private async Task<Coverage> GetCoverage(int userId)
    {
        var coverages = await _coveragesService.GetAllAsync(userId);
        
        var coverage = coverages.Where(o => o.Status == CoverageStatus.Active).MinBy(o => o.Priority);

        if (coverage is null)
        {
            throw new AppException(HttpStatusCode.NotFound,
                $"Active coverage could not be found for user = {userId}");
        }

        return coverage;
    }

    private async Task<InsConfigService> GetServiceConfig(
        int practiceId, 
        int insuranceId,
        Appointment appointment)
    {
        var insuranceConfig = (await _insuranceConfigsService.GetAsync(
            practiceId: practiceId,
            insuranceId: insuranceId)).FirstOrDefault();

        if (insuranceConfig is null)
        {
            throw new AppException(HttpStatusCode.NotFound,
                $"Insurance configuration not found for insurance id = {insuranceId} and practice id = {practiceId}");
        }
        
        var serviceConfig = insuranceConfig.ServiceConfigurations.FirstOrDefault(GetServiceTypePredicate(appointment));

        if (serviceConfig is null)
        {
            throw new AppException(HttpStatusCode.NotFound,
                $"Service configuration not found for insurance config id = {insuranceConfig.GetId()} and type = {InsuranceServiceType.ProviderVisit}");
        }

        return serviceConfig;
    }

    private Func<InsConfigService, bool> GetServiceTypePredicate(Appointment appointment)
    {
        var providerVisitType = new[] { AppointmentWithType.Provider, AppointmentWithType.HealthCoachAndProvider };

        if (!providerVisitType.Contains(appointment.WithType))
        {
            return o => o.Type == InsuranceServiceType.HealthCoachVisit;
        }

        if (appointment.Purpose == AppointmentPurpose.SickVisit)
        {
            return o => o.Type == InsuranceServiceType.SickVisit 
                        && o.MinimumVisitLength >= appointment.Duration 
                        && o.MaximumVisitLength <= appointment.Duration;
        }
        
        return o => o.Type == InsuranceServiceType.ProviderVisit;
    }

    private NotesContentModel GetNoteContent(Note note)
    {
        var noteContent = JsonConvert.DeserializeObject<NotesContentModel>(note.Content.Content);

        if (noteContent is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Notes contents not found for note id = {note.GetId()}");
        }

        return noteContent;
    }

    private async Task<Claim> CreateClaim(
        User user,
        Employee employee,
        Coverage coverage,
        Note note,
        NotesContentModel noteContent,
        MdmCode mdmCode,
        InsConfigService serviceConfig,
        PracticeEntity practiceEntity,
        ClaimStatus claimStatus)
    {
        var cptCode = !string.IsNullOrEmpty(serviceConfig.ClaimSubmissionConfig.CptCode)
            ? serviceConfig.ClaimSubmissionConfig.CptCode
            : mdmCode.Code;
            
        var diagnosis = noteContent.Diagnosis;

        var patientAddress = string.Join(" ", new [] { user.BillingAddress.StreetAddress1, user.BillingAddress.StreetAddress2 });

        var claim = new Claim
        {
            ClaimantUniversalId = note.UniversalId,
            ClaimantNote = note,
            ClaimStatus = claimStatus,
            SubscriberId = coverage.MemberId,
            PatientLastName = user.LastName.ToUpper(),
            PatientFirstName = user.FirstName.ToUpper(),
            PatientBirthday = user.Birthday ?? DateTime.Now,
            PatientAddress = patientAddress,
            PatientCity = user.BillingAddress.City.ToUpper(),
            PatientState = user.BillingAddress.State.ToUpper(),
            PatientZip = user.BillingAddress.ZipCode,
            BillingTaxId = practiceEntity.TaxId,
            BillingTaxIdType = "E",
            BillingName = practiceEntity.BillingName,
            BillingAddress1 = practiceEntity.BillingAddress.StreetAddress1,
            BillingCity = practiceEntity.BillingAddress.City,
            BillingState = practiceEntity.BillingAddress.State,
            BillingZip = practiceEntity.BillingAddress.ZipCode,
            BillingNpi = practiceEntity.Npi,
            PayerName = coverage.Insurance.Name,
            Procedure = new ClaimProcedure(
                serviceFrom: note.VisitDate,
                placeOfService: serviceConfig.ClaimSubmissionConfig.PlaceOfService,
                procedureCode: cptCode,
                diagnosisPointers: string.Join("", diagnosis.Select((o, i) => Convert.ToChar(i + 65))),
                chargeAmount: serviceConfig.Fee.ToString("F2"),
                units: 1,
                renderingProviderNpi: employee.Npi),
            RenderingProviderFirstName = employee.User.FirstName.ToUpper(),
            RenderingProviderLastName = employee.User.LastName.ToUpper(),
            RenderingProviderSignatureDate = note.CompletedAt.GetValueOrDefault()
        };

        var claimDomain = ClaimDomain.Create(claim);
        
        foreach (var diagnosisModel in diagnosis)
        {
            claimDomain.AddDiagnosis(diagnosisModel);
        }

        claim = await _claimsService.CreateAsync(claim);

        return claim;
    }

    #endregion
}
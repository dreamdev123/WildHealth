using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Notes;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Products;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Domain.Models.Employees;
using WildHealth.Domain.Models.Insurances;
using WildHealth.Domain.Models.Notes;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Services;
using WildHealth.Settings;
using ClaimStatus = WildHealth.Domain.Enums.Insurance.ClaimStatus;
using IntegrationVendor = WildHealth.Domain.Enums.Integrations.IntegrationVendor;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class SyncNoteWithPmCommandHandler : IRequestHandler<SyncNoteWithPmCommand>
{
    private static readonly string[] SettingsKeys =
    {
        SettingsNames.General.ApplicationBaseUrl,
    };
    
    private readonly IAppointmentsService _appointmentsService;
    private readonly INoteService _noteService;
    private readonly IEmployeeService _employeeService;
    private readonly IPatientsService _patientsService;
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
    private readonly IIntegrationsService _integrationsService;
    private readonly IMediator _mediator;
    private readonly ILocationsService _locationsService;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly ISettingsManager _settingsManager;
    private readonly AppOptions _appOptions;
    private readonly ILogger<SyncNoteWithPmCommandHandler> _logger;

    public SyncNoteWithPmCommandHandler(
        IAppointmentsService appointmentsService,
        INoteService noteService,
        IEmployeeService employeeService,
        IPatientsService patientsService,
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory,
        IIntegrationsService integrationsService,
        IMediator mediator,
        ILocationsService locationsService,
        IFeatureFlagsService featureFlagsService,
        ISettingsManager settingsManager,
        IOptions<AppOptions> appOptions,
        ILogger<SyncNoteWithPmCommandHandler> logger)
    {
        _appointmentsService = appointmentsService;
        _noteService = noteService;
        _employeeService = employeeService;
        _patientsService = patientsService;
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        _integrationsService = integrationsService;
        _mediator = mediator;
        _locationsService = locationsService;
        _featureFlagsService = featureFlagsService;
        _settingsManager = settingsManager;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task Handle(SyncNoteWithPmCommand command, CancellationToken cancellationToken)
    {
        var note = await _noteService.GetByIdAsync(command.NoteId);

        var appointment = note.Appointment;
        var appointmentDomain = AppointmentDomain.Create(appointment);
        
        var practiceId = note.Patient.User.PracticeId;
        
        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(appointment.Patient.User.PracticeId);
        
        var patient = await _patientsService.GetByIdAsync(note.PatientId, PatientSpecifications.PatientWithSubscriptionAndIntegrations);

        if (!appointmentDomain.IsInsurance())
        {
            return;
        }
        
        var location = await _locationsService.GetByIdAsync(NoteDomain.Create(note).GetLocationId(), practiceId);
        var employee = await _employeeService.GetByIdAsync(note.EmployeeId, EmployeeSpecifications.WithUserAndIntegrations);

        if (!appointmentDomain.IsLinkedWithIntegrationSystem(vendor: pmService.Vendor, purpose: IntegrationPurposes.Appointment.ExternalId))
        {
            try
            {
                await CreatePmAppointment(
                    pmService: pmService,
                    appointment: appointment,
                    employee: employee,
                    patient: patient,
                    location: location,
                    practiceId: practiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to sync appointment with id {appointment.GetId()} to pm: {ex}");
                
                return;
            }
        }

        if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.ClaimsAutomation))
        {
            return;
        }

        Claim? claim;
        
        try
        {
            claim = note.Claims.FirstOrDefault() ?? await _mediator.Send(new CreateClaimCommand(noteId: note.GetId(), claimStatus: ClaimStatus.Created));

            if (claim is null)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Can't create claim for incomplete note with note id = {note.GetId()} {ex}");
            return;
        }
        
        
        if (!ClaimDomain.Create(claim).IsLinkedWithIntegrationSystem(vendor: pmService.Vendor, purpose: IntegrationPurposes.Claim.ExternalId))
        {
            try
            {
                await CreatePmClaim(
                    pmService: pmService,
                    appointment: appointment,
                    patient: patient,
                    employee: employee,
                    location: location,
                    claim: claim,
                    practiceId: practiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to sync claim with id {claim.GetId()} to pm: {ex}");
            }
        }
    }

    #region private

    private async Task<string> CreatePmAppointment(
        IPracticeManagementIntegrationService pmService,
        Appointment appointment,
        Employee employee,
        Patient patient,
        Location location,
        int practiceId)
    {
        var appointmentInsuranceType = await _appointmentsService.GetAppointmentInsuranceTypeByAppointmentPurpose(appointment.Purpose);

        if (appointmentInsuranceType is null)
        {
            _logger.LogError($"An appointment with id {appointment.GetId()} location is required to link to PM");

            return string.Empty;
        }
        
        var patientProfileLink = await FormatLink(patient);

        var appointmentPmId = await pmService.SyncAppointment(
            startDate: appointment.StartDate,
            endDate: appointment.EndDate,
            duration: appointment.Duration,
            employeeName: employee.User.GetFullname(),
            employeePmRef: EmployeeDomain.Create(employee).GetIntegrationId(IntegrationVendor.OpenPm, IntegrationPurposes.Employee.ArpId),
            patientName: patient.User.GetFullname(),
            patientPmRef: patient.User.GetIntegrationId(IntegrationVendor.OpenPm, IntegrationPurposes.User.Customer),
            locationPmRef: location.GetIntegrationId(IntegrationVendor.OpenPm),
            code: appointmentInsuranceType.InsuranceCode,
            comment: patientProfileLink,
            practiceId: practiceId);

        var appointmentIntegration = new AppointmentIntegration(
            vendor: pmService.Vendor,
            purpose: IntegrationPurposes.Appointment.ExternalId,
            value: appointmentPmId,
            appointment: appointment);

        await _integrationsService.CreateAsync(appointmentIntegration);

        return appointmentPmId;
    }

    private async Task<string> CreatePmClaim(
        IPracticeManagementIntegrationService pmService,
        Appointment appointment,
        Patient patient,
        Employee employee,
        Location location,
        Claim claim,
        int practiceId)
    {
        var claimPmId = await pmService.CreateClaimAsync(
            appointment: appointment,
            location: location,
            patient: patient,
            employee: employee,
            claim: claim,
            practiceId: practiceId);
            
        var claimIntegration = new ClaimIntegration(
            vendor: pmService.Vendor,
            purpose: IntegrationPurposes.Claim.ExternalId,
            value: claimPmId,
            claim: claim);

        await _integrationsService.CreateAsync(claimIntegration);
        
        return claimPmId;
    }

    private async Task<string> FormatLink(Patient patient)
    {
        
        var settings = await _settingsManager.GetSettings(SettingsKeys, patient.User.PracticeId);
        var appUrl = settings[SettingsNames.General.ApplicationBaseUrl];

        return string.Format(_appOptions.PatientProfileUrl, appUrl, patient.GetId());
    }

    #endregion
}
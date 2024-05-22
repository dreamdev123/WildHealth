using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Integrations;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Employees;
using WildHealth.Fhir.Models.Practitioners;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Factories.IntegrationServiceFactory;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class SyncProviderWithOpenPmCommandHandler : IRequestHandler<SyncProviderWithOpenPmCommand>
{
    private readonly IEmployeeService _employeeService;
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
    private readonly IIntegrationsService _integrationsService;
    private readonly ILogger<SyncProviderWithOpenPmCommandHandler> _logger;

    public SyncProviderWithOpenPmCommandHandler(
        IEmployeeService employeeService,
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory,
        IIntegrationsService integrationsService,
        ILogger<SyncProviderWithOpenPmCommandHandler> logger)
    {
        _employeeService = employeeService;
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        _integrationsService = integrationsService;
        _logger = logger;
    }

    public async Task Handle(SyncProviderWithOpenPmCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Synchronizing provider employee with id {command.EmployeeId} to OpenPM has started.");

        var employee = await _employeeService.GetByIdAsync(command.EmployeeId, EmployeeSpecifications.WithUserAndIntegrations);

        if (employee.RoleId != Roles.ProviderId || string.IsNullOrEmpty(employee.Npi))
        {
            return;
        }
        
        try
        {
            await SyncPrimaryCarePhysician(employee);

            await SyncAppointmentResourceProvider(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Synchronizing provider employee with id {employee.GetId()} with OpenPm failed: {ex.Message}");
        }
        
        _logger.LogInformation($"Synchronizing provider employee with id {command.EmployeeId} to OpenPM has finished.");
    }

    #region private

    private async Task SyncPrimaryCarePhysician(Employee employee)
    {
        var employeeDomain = EmployeeDomain.Create(employee);
        var pcpIntegrationValue = employeeDomain.GetIntegrationId(IntegrationVendor.OpenPm, IntegrationPurposes.Employee.BilId);
        
        if (string.IsNullOrEmpty(pcpIntegrationValue))
        {
            var practitioner = await GetOpenPmPractitionerByNpi(
                employee.Npi,
                OpenPmConstants.Practitioner.Type.RenderingProvider,
                employee.User.PracticeId);

            if (practitioner is not null)
            {
                await CreateIntegration(practitioner.Id, IntegrationPurposes.Employee.BilId, employee);
            }
            else
            {
                var practitionerId = await CreatePractitionerInOpenPmAsync(employee, OpenPmConstants.Practitioner.Type.RenderingProvider);
                await CreateIntegration(practitionerId, IntegrationPurposes.Employee.BilId, employee);
            }
        }
    }

    private async Task SyncAppointmentResourceProvider(Employee employee)
    {
        var employeeDomain = EmployeeDomain.Create(employee);
        var arpIntegrationValue = employeeDomain.GetIntegrationId(IntegrationVendor.OpenPm, IntegrationPurposes.Employee.ArpId);

        if (string.IsNullOrEmpty(arpIntegrationValue))
        {
            var appointmentResourceProvider = await GetOpenPmPractitionerByNpi(
                employee.Npi,
                OpenPmConstants.Practitioner.Type.AppointmentResourceProvider,
                employee.User.PracticeId);

            if (appointmentResourceProvider is not null)
            {
                var arpId = CleanArpId(appointmentResourceProvider.Id);
                
                await CreateIntegration(arpId, IntegrationPurposes.Employee.ArpId, employee);
            }
            else
            {
                var arpId = await CreatePractitionerInOpenPmAsync(employee, OpenPmConstants.Practitioner.Type.AppointmentResourceProvider);

                arpId = CleanArpId(arpId);
                
                await CreateIntegration(arpId, IntegrationPurposes.Employee.ArpId, employee);
            }
        }
    }

    private string CleanArpId(string arpId)
    {
        // This is because OpenPM is returning "aprP" as the prefix to this identifier.  However, the extra "P" is not correct and fails when trying to pass that to OpenPM to
        // create an appointment
        if (arpId.Length > 8)
        {
            return arpId.Replace("aprP", "apr");
        }

        return arpId;
    }

    private async Task CreateIntegration(string value, string purpose, Employee employee)
    {
        var integration = new EmployeeIntegration(
            IntegrationVendor.OpenPm,
            purpose,
            value,
            employee);
            
        await _integrationsService.CreateAsync(integration);
    }

    private async Task<PractitionerModel?> GetOpenPmPractitionerByNpi(string npi, string practitionerType, int practiceId)
    {
        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(practiceId);
        
        var practitioners = await pmService.QueryPractitioners(practitionerType, practiceId);

        var practitioner = practitioners
            .FirstOrDefault(x =>
                x.Resource.Identifiers.FirstOrDefault(t => t.Type.Text == "NPI Number")?.Value == npi);

        return practitioner?.Resource;
    }

    private async Task<string> CreatePractitionerInOpenPmAsync(Employee employee, string practitionerType)
    {
        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(employee.User.PracticeId);

        var user = employee.User;
        var practitionerId = await pmService.CreatePractitioner(
            user.FirstName,
            user.LastName,
            employee.Npi,
            user.Gender,
            user.Birthday ?? DateTime.Now,
            practitionerType,
            user.PracticeId);

        return practitionerId;
    }

    #endregion
}
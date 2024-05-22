using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Employees;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using MediatR;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Models.Patient;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class UpdatePatientInIntegrationSystemOnPatientUpdatedEvent : INotificationHandler<PatientUpdatedEvent>
    {
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        private readonly IEmployeeService _employeeService;
        private readonly IPatientsService _patientsService;

        public UpdatePatientInIntegrationSystemOnPatientUpdatedEvent(
            IIntegrationServiceFactory integrationServiceFactory,
            IEmployeeService employeeService,
            IPatientsService patientsService)
        {
            _integrationServiceFactory = integrationServiceFactory;
            _employeeService = employeeService;
            _patientsService = patientsService;
        }

        public async Task Handle(PatientUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(notification.PatientId);
            var patientDomain = PatientDomain.Create(patient);
            
            var integrationService = await _integrationServiceFactory.CreateAsync(patientDomain.GetPracticeId());

            if (!patientDomain.IsLinkedWithIntegrationSystem(integrationService.IntegrationVendor))
            {
                return;
            }
            
            var assigmentEmployees = await _employeeService.GetAssignedToAsync(patientDomain.GetId());
            
            await integrationService.UpdatePatientAsync(patient, assigmentEmployees);
        }
    }
}
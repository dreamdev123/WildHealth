using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using MediatR;
using WildHealth.Application.Services.Patients;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendNotificationOnPatientTransferredToLocationEvent : INotificationHandler<PatientTransferredToLocationEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IEmployeeService _employeeService;
        private readonly IPatientsService _patientsService;

        public SendNotificationOnPatientTransferredToLocationEvent(
            INotificationService notificationService, 
            IEmployeeService employeeService,
            IPatientsService patientsService)
        {
            _notificationService = notificationService;
            _employeeService = employeeService;
            _patientsService = patientsService;
        }

        public async Task Handle(PatientTransferredToLocationEvent @event, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(@event.PatientId);
            
            var location = patient.Location;

            var locationCoordinator = location.Employees.FirstOrDefault(i => i.IsCareCoordinator);

            if (locationCoordinator is null)
            {
                return;
            }

            var employee = await _employeeService.GetByIdAsync(locationCoordinator.EmployeeId);
            
            var notification = new PatientTransferredNotification(patient, employee);

            await _notificationService.CreateNotificationAsync(notification);
        }
    }
}
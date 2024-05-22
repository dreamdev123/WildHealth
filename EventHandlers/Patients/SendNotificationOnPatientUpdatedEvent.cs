using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using MediatR;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Patients;

namespace WildHealth.Application.EventHandlers.Patients
{
    /// <summary>
    /// Provides notification sender on patient updated event
    /// </summary>
    public class SendNotificationOnPatientUpdatedEvent : INotificationHandler<PatientUpdatedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IEmployeeService _employeeService;
        private readonly IPatientsService _patientsService;

        public SendNotificationOnPatientUpdatedEvent(
            INotificationService notificationService,
            IEmployeeService employeeService,
            IPatientsService patientsService)
        {
            _notificationService = notificationService;
            _employeeService = employeeService;
            _patientsService = patientsService;
        }
        
        /// <summary>
        /// Handles event and send notification
        /// </summary>
        /// <param name="event"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Handle(PatientUpdatedEvent @event, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(@event.PatientId);
            var newlyAssignedEmployees = await _employeeService.GetActiveAsync(
                ids: @event.NewlyAssignedEmployeeIds,
                practiceId: patient.User.PracticeId,
                locationId: patient.LocationId);
            
            var notification = new PatientAssigmentNotification(patient, newlyAssignedEmployees);
            
            await _notificationService.CreateNotificationAsync(notification);
        }
    }
}
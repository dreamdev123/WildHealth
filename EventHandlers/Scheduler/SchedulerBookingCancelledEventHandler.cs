using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.Scheduler;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.CommandHandlers.Appointments.Flows;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Enums.Appointments;
using MediatR;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Constants;

namespace WildHealth.Application.EventHandlers.Scheduler
{
    public class SchedulerBookingCancelledEventHandler : INotificationHandler<SchedulerBookingCancelledEvent>
    {
        private readonly IPatientProfileService _patientProfileService;
        private readonly IAppointmentsService _appointmentsService;
        private readonly IFlowMaterialization _materializeFlow;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEmployeeService _employeeService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public SchedulerBookingCancelledEventHandler(
            IPatientProfileService patientProfileService,
            IAppointmentsService appointmentsService,
            IFlowMaterialization materializeFlow,
            IDateTimeProvider dateTimeProvider,
            IEmployeeService employeeService, 
            IMediator mediator,
            ILogger<SchedulerBookingCancelledEventHandler> logger)
        {
            _patientProfileService = patientProfileService;
            _appointmentsService = appointmentsService;
            _materializeFlow = materializeFlow;
            _dateTimeProvider = dateTimeProvider;
            _employeeService = employeeService;
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task Handle(SchedulerBookingCancelledEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Cancelling appointment with id {notification.SchedulerBookingId} is started");

            var appointment = await _appointmentsService.GetBySchedulerSystemIdAsync(notification.SchedulerBookingId);
            if (appointment is null)
            {
                _logger.LogInformation($"Cannot find appointment with booking id {notification.SchedulerBookingId}, this is potentially due to our current scheduling system (TimeKit) uses a single instance to support dev/staging/prod environments.  This ID is likely an ID from a different environment.");
                return;
            }

            var cancelledBy = await _employeeService.GetBySchedulerAccountIdAsync(notification.SchedulerUserId);
            
            var patientProfileUrl = appointment.Patient is not null
                ? await _patientProfileService.GetProfileLink(
                    patientId: appointment.Patient.GetId(), 
                    practiceId: appointment.Patient.User.PracticeId
                ) : string.Empty;

            var flow = new CancelAppointmentFlow(
                appointment: appointment,
                cancelledBy: cancelledBy?.User,
                cancelledAt: _dateTimeProvider.UtcNow(),
                patientProfileUrl: patientProfileUrl,
                reason: AppointmentCancellationReason.Cancelled
            );

            await flow.Materialize(_materializeFlow.Materialize);
            
            var appointmentCancelledEvent = new AppointmentCancelledEvent(appointment, source: ClientConstants.Source.Clarity);
            await _mediator.Publish(appointmentCancelledEvent, cancellationToken);
            _logger.LogInformation($"Cancelling appointment with id {notification.SchedulerBookingId} was successfully finished");
        }
    }
}
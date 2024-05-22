using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Schedulers.Bookings;
using WildHealth.Application.Services.Schedulers.Meetings;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Zoom.Clients.Exceptions;
using WildHealth.Application.Commands.Products;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.CommandHandlers.Appointments.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using MediatR;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Constants;

namespace WildHealth.Application.CommandHandlers.Appointments
{
    public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Appointment>
    {
        private readonly ISchedulerBookingsService _schedulerBookingsService;
        private readonly ISchedulerMeetingsService _schedulerMeetingsService;
        private readonly IPatientProfileService _patientProfileService;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IAppointmentsService _appointmentsService;
        private readonly IFlowMaterialization _materializeFlow;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUsersService _usersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CancelAppointmentCommandHandler(
            ISchedulerBookingsService schedulerBookingsService,
            ISchedulerMeetingsService schedulerMeetingsService,
            IPatientProfileService patientProfileService,
            ILogger<CancelAppointmentCommandHandler> logger,
            IFeatureFlagsService featureFlagsService,
            IAppointmentsService appointmentsService, 
            IFlowMaterialization materializeFlow,
            IDateTimeProvider dateTimeProvider, 
            IUsersService usersService,
            IMediator mediator)
        {
            _schedulerBookingsService = schedulerBookingsService;
            _schedulerMeetingsService = schedulerMeetingsService;
            _patientProfileService = patientProfileService;
            _featureFlagsService = featureFlagsService;
            _appointmentsService = appointmentsService;
            _materializeFlow = materializeFlow;
            _dateTimeProvider = dateTimeProvider;
            _usersService = usersService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Appointment> Handle(CancelAppointmentCommand command, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentsService.GetByIdAsync(command.Id);

            var cancelledBy = await _usersService.GetByIdAsync(command.CancelledBy);

            var patientProfileUrl = appointment.Patient is not null
                ? await _patientProfileService.GetProfileLink(
                    patientId: appointment.Patient.GetId(), 
                    practiceId: appointment.Patient.User.PracticeId
                ) : string.Empty;
            
            var flow = new CancelAppointmentFlow(
                appointment: appointment,
                cancelledBy: cancelledBy,
                cancelledAt: _dateTimeProvider.UtcNow(),
                patientProfileUrl: patientProfileUrl,
                reason: command.CancellationReason
            );

            await flow.Materialize(_materializeFlow.Materialize);

            await CancelAppointmentInSchedulerSystem(appointment);

            await DeleteFromMeetingService(appointment);
            
            if (_featureFlagsService.GetFeatureFlag(FeatureFlags.PatientProduct))
            {
                await VoidProductAsync(appointment.ProductId);
            }
            
            var source = command.Source ?? ClientConstants.Source.MobileApp;

            var appointmentCancelledEvent = new AppointmentCancelledEvent(appointment, source);
            await _mediator.Publish(appointmentCancelledEvent, cancellationToken);
            
            return appointment;
        }

        private async Task CancelAppointmentInSchedulerSystem(Appointment appointment)
        {
            var appointmentDomain = AppointmentDomain.Create(appointment);

            var isPast = appointmentDomain.IsPast(_dateTimeProvider.UtcNow());

            if (isPast) {
                return;
            }

            var employee = appointment.Employees
                .Select(x => x.Employee)
                .FirstOrDefault();

            if (employee is null)
            {
                _logger.LogError($"Appointment with id {appointment.Id} does not any employee");
                return;
            }
            
            if (string.IsNullOrEmpty(appointment.SchedulerSystemId))
            {
                _logger.LogError($"Appointment with id {appointment.Id} does not have SchedulerId");
                return;
            }

            await _schedulerBookingsService.CancelBookingAsync(employee.User.PracticeId, appointment);
        }

        private async Task DeleteFromMeetingService(Appointment appointment)
        {
            var appointmentDomain = AppointmentDomain.Create(appointment);

            var meetingOwner = appointmentDomain.GetMeetingOwner();

            var isPast = appointmentDomain.IsPast(_dateTimeProvider.UtcNow());

            if (isPast) {
                return;
            }

            if (meetingOwner is null)
            {
                _logger.LogError($"Appointment with id {appointment.Id} does not have meeting owner");
                return;
            }
            
            var practiceId = meetingOwner.User.PracticeId;

            if (!appointment.MeetingSystemId.HasValue)
            {
                _logger.LogWarning($"Appointment with id {appointment.Id} is not liked to meeting system");
                return;
            }

            try
            {
                await _schedulerMeetingsService.DeleteMeetingAsync(practiceId, appointment.MeetingSystemId.Value, meetingOwner.User.Email);
            }
            catch (ZoomException e)
            {
                _logger.LogError($"Unable to delete meeting: {appointment.MeetingSystemId.Value} for appointment: {appointment.GetId()}. with error: {e.ToString()}");

                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return;
                }

                throw;
            }
        }

        /// <summary>
        /// Void (un use) product
        /// </summary>
        /// <param name="productId"></param>
        private async Task VoidProductAsync(int? productId)
        {
            if (productId is null)
            {
                return;
            }

            var command = new VoidProductCommand(productId.Value);

            await _mediator.Send(command);
        }
    }
}
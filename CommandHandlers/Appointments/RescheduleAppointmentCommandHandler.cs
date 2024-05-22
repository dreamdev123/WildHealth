using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Domain.Entities.Appointments;
using MediatR;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Constants;

namespace WildHealth.Application.CommandHandlers.Appointments
{
    public class RescheduleAppointmentCommandHandler : IRequestHandler<RescheduleAppointmentCommand, Appointment>
    {
        private readonly IAppointmentsService _appointmentsService;
        private readonly IMediator _mediator;

        public RescheduleAppointmentCommandHandler(
            IAppointmentsService appointmentsService,
            IMediator mediator)
        {
            _appointmentsService = appointmentsService;
            _mediator = mediator;
        }
        
        public async Task<Appointment> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentsService.GetByIdAsync(request.CancelledAppointmentId);
            
            if (request.IsPatientRequesting && appointment.EndDate <= DateTime.Now)
            {
                throw new DomainException($"The past appointment with id {appointment.Id} cannot be rescheduled.");
            }
            
            var source = request.Source ?? ClientConstants.Source.MobileApp;
            
            var cancelCommand = new CancelAppointmentCommand(request.CancelledAppointmentId, request.CreatedById, AppointmentCancellationReason.Reschedule, source);
            await _mediator.Send(cancelCommand, cancellationToken);

            var createCommand = new CreateAppointmentCommand(
                practiceId: request.PracticeId,
                employeeIds: request.EmployeeIds,
                patientId: appointment.PatientId,
                locationId: request.LocationId,
                startDate: request.StartDate,
                endDate: request.EndDate,
                locationType: request.LocationType,
                appointmentTypeId: request.AppointmentTypeId,
                appointmentTypeConfigurationId: request.AppointmentTypeConfigurationId,
                name: request.Name,
                comment: request.Comment,
                timeZoneId: request.TimeZoneId,
                userType: request.UserType,
                createdById: request.CreatedById,
                reason: request.Reason,
                reasonType: request.ReasonType,
                isRescheduling: true,
                replacedAppointmentId: appointment.GetId(),
                source: source
            );
            
            var newAppointment = await _mediator.Send(createCommand, cancellationToken);
            
            return newAppointment!;
        }
    }
}
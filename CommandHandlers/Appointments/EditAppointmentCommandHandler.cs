using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Shared.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Appointments
{
    public class EditAppointmentCommandHandler : IRequestHandler<EditAppointmentCommand, Appointment>
    {
        private readonly IAppointmentsService _appointmentsService;
        public EditAppointmentCommandHandler(IAppointmentsService appointmentsService)
        {
            _appointmentsService = appointmentsService;
        }
        
        public async Task<Appointment> Handle(EditAppointmentCommand request, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentsService.GetByIdAsync(request.Id);
            
            AssertPatientUpdatePermission(appointment, request.PatientId);

            appointment.Comment = request.Comment;
            
            await _appointmentsService.EditAppointmentAsync(appointment);

            return appointment;
        }

        private void AssertPatientUpdatePermission(Appointment appointment, int? requestPatientId)
        {
            if (requestPatientId.HasValue && appointment.PatientId != requestPatientId)
            {
                throw new AppException(HttpStatusCode.Forbidden,
                    "You do not have permissions to update this appointment");
            }
        }
    }
}
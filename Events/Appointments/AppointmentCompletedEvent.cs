using WildHealth.Domain.Entities.Appointments;
using WildHealth.Shared.Enums;
using MediatR;

namespace WildHealth.Application.Events.Appointments
{
    public class AppointmentCompletedEvent : INotification
    {
        public Appointment Appointment { get; }

        public UserType CreatedBy { get; }

        public AppointmentCompletedEvent(Appointment appointment, UserType createdBy)
        {
            Appointment = appointment;
            CreatedBy = createdBy;
        }
    }
}

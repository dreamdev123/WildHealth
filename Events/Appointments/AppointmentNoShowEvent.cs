using WildHealth.Domain.Entities.Appointments;
using MediatR;

namespace WildHealth.Application.Events.Appointments
{
    public class AppointmentNoShowEvent : INotification
    {
        public Appointment Appointment { get; }

        public AppointmentNoShowEvent(Appointment appointment)
        {
            Appointment = appointment;
        }
    }
}

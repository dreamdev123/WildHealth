using WildHealth.Domain.Entities.Appointments;
using MediatR;

namespace WildHealth.Application.Events.Appointments
{
    public class AppointmentCancelledEvent : INotification
    {
        public Appointment Appointment { get; }
        public string Source { get; }
        
        public AppointmentCancelledEvent(Appointment appointment, string source)
        {
            Appointment = appointment;
            Source = source;
        }
    }
}
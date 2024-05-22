using WildHealth.Shared.Enums;
using MediatR;

namespace WildHealth.Application.Events.Appointments
{
    public class AppointmentCreatedEvent : INotification
    {
        public int AppointmentId { get; }
        public UserType CreatedBy { get; } 
        public int? PatientId { get; }
        public bool IsRescheduling { get; }
        public string Source { get; }

        public AppointmentCreatedEvent(int appointmentId, UserType createdBy, bool isRescheduling, string source, int? patientId = null)
        {
            AppointmentId = appointmentId;
            CreatedBy = createdBy;
            IsRescheduling = isRescheduling;
            PatientId = patientId;
            Source = source;
        }
    }
}

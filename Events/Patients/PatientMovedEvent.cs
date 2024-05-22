using MediatR;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Events.Patients
{
    public class PatientMovedEvent : INotification
    {
        public Patient OldPatient { get; }
        public Patient NewPatient { get; }
        
        public PatientMovedEvent(Patient oldPatient, Patient newPatient)
        {
            OldPatient = oldPatient;
            NewPatient = newPatient;
        }
    }
}
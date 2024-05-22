using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using MediatR;

namespace WildHealth.Application.Events.Patients
{
    public class PatientMigratedFromIntegrationSystemEvent : INotification
    {
        public PatientMigratedFromIntegrationSystemEvent(
            Patient patient, 
            Subscription subscription,
            bool dpc)
        {
            Patient = patient;
            Subscription = subscription;
            DPC = dpc;
        }

        public Patient Patient { get; }

        public Subscription Subscription { get; }

        public bool DPC { get; }
    }
}
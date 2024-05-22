using System.Collections.Generic;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using MediatR;

namespace WildHealth.Application.Events.Payments
{
    public class SubscriptionChangedEvent : INotification
    {
        public Patient Patient { get; }

        public Subscription NewSubscription { get; }

        public Subscription PreviousSubscription { get; }

        public IEnumerable<int> PatientAddOnIds { get; }

        public SubscriptionChangedEvent(
            Patient patient, 
            Subscription newSubscription,
            Subscription previousSubscription,
            IEnumerable<int> patientAddOnIds)
        {
            Patient = patient;
            NewSubscription = newSubscription;
            PreviousSubscription = previousSubscription;
            PatientAddOnIds = patientAddOnIds;
        }
    }
}

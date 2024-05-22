using MediatR;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Events.Subscriptions;

public class SubscriptionActivatedEvent : INotification
{
    public SubscriptionActivatedEvent(
        Patient patient, 
        Subscription newSubscription, 
        Subscription previousSubscription)
    {
        Patient = patient;
        NewSubscription = newSubscription;
        PreviousSubscription = previousSubscription;
    }

    public Patient Patient { get; }
    
    public Subscription NewSubscription { get; }
    
    public Subscription PreviousSubscription { get; }
}
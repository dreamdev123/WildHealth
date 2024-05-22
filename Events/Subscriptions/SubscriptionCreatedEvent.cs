using MediatR;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Events.Subscriptions;

public class SubscriptionCreatedEvent : INotification
{
    public SubscriptionCreatedEvent(Patient patient)
    {
        Patient = patient;
    }

    public Patient Patient { get; }
}
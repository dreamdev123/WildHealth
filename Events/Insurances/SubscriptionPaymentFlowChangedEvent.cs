

using System;
using MediatR;

namespace WildHealth.Application.Events.Insurances;

public class SubscriptionPaymentFlowChangedEvent : INotification
{
    public int SubscriptionId { get; }
    public string PriorFlow { get; }
    public string NewFlow { get; }

    public SubscriptionPaymentFlowChangedEvent(int subscriptionId, string priorFlow, string newFlow)
    {
        SubscriptionId = subscriptionId;
        PriorFlow = priorFlow;
        NewFlow = newFlow;
    }
}
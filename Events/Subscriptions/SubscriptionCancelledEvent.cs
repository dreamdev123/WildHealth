using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using MediatR;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Infrastructure.Communication.Messages;

namespace WildHealth.Application.Events.Subscriptions
{
    public class SubscriptionCancelledEvent : INotification, IEvent
    {
        public Patient Patient { get; }
        
        public Subscription Subscription { get; }
        
        public PaymentPlan PaymentPlan { get; }
        
        public CancellationReasonType CancellationReasonType { get; }

        public bool IsRenewal =>
            CancellationReasonType is CancellationReasonType.Renewed or CancellationReasonType.Replaced;
        
        public SubscriptionCancelledEvent(
            Patient patient, 
            Subscription subscription,
            PaymentPlan paymentPlan,
            CancellationReasonType cancellationReasonType)
        {
            Patient = patient;
            Subscription = subscription;
            PaymentPlan = paymentPlan;
            CancellationReasonType = cancellationReasonType;
        }
    }
}
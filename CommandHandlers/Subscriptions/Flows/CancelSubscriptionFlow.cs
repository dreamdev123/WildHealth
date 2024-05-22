using System;
using System.Linq;
using WildHealth.Application.Domain.PaymentIssues;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Models.Timeline.Subscription;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public class CancelSubscriptionFlow : IMaterialisableFlow
{
    private readonly Subscription _subscription;
    private readonly CancellationReasonType _reasonType;
    private readonly string _reason;
    private readonly DateTime _utcNow;
    private readonly PaymentIssue[] _correspondingPaymentIssues;

    public CancelSubscriptionFlow(
        Subscription subscription, 
        CancellationReasonType reasonType,
        string reason,
        DateTime utcNow,
        PaymentIssue[] correspondingPaymentIssues)
    {
        _subscription = subscription;
        _reasonType = reasonType;
        _reason = reason;
        _utcNow = utcNow;
        _correspondingPaymentIssues = correspondingPaymentIssues;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_subscription.NotCanceled())
        {
            _subscription.Cancel(
                now: _utcNow, 
                reasonType: _reasonType,
                reason: _reason
            );

            return _subscription.Updated() + FireEventUnlessTechnicalReason() + CancelCorrespondingPaymentIssues();
        }

        return MaterialisableFlowResult.Empty;
    }
    
    private MaterialisableFlowResult CancelCorrespondingPaymentIssues()
    {
        return _correspondingPaymentIssues
            .Where(x => x.Type == PaymentIssueType.Subscription)
            .Select(x => x.UserCancel())
            .Aggregate(MaterialisableFlowResult.Empty, (cur, acc) => cur + acc);
    }

    private MaterialisableFlowResult FireEventUnlessTechnicalReason() => 
        _reasonType != CancellationReasonType.Renewed && _reasonType != CancellationReasonType.Replaced ? 
            new SubscriptionCancelledTimelineEvent(_subscription.PatientId, _utcNow).Added() + new SubscriptionCancelledEvent(_subscription.Patient, _subscription, _subscription.PaymentPrice.PaymentPeriod.PaymentPlan, _reasonType) :
            MaterialisableFlowResult.Empty;
}
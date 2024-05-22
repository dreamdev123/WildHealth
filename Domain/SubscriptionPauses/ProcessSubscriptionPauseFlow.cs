using System;
using System.Linq;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.Domain.SubscriptionPauses;

public record ProcessSubscriptionPauseFlow
    (SubscriptionPause Pause, Subscription Subscription, DateTime UtcNow) : IMaterialisableFlow
{
    private readonly Dictionary<SubscriptionPauseStatus, SubscriptionPauseStatus[]> _transitionMatrix = new()
    {
        { SubscriptionPauseStatus.Pending, new[] { SubscriptionPauseStatus.Active, SubscriptionPauseStatus.Terminated } },
        { SubscriptionPauseStatus.Active, new[] { SubscriptionPauseStatus.Ended, SubscriptionPauseStatus.Terminated } },
        { SubscriptionPauseStatus.Ended, Array.Empty<SubscriptionPauseStatus>() },
        { SubscriptionPauseStatus.Terminated, Array.Empty<SubscriptionPauseStatus>() },
    };

    public MaterialisableFlowResult Execute()
    {
        var nextStatus = DetermineNextStatus();

        if (!IsNextStatusValid(Pause.Status, nextStatus))
        {
            return MaterialisableFlowResult.Empty;
        }

        return nextStatus switch
        {
            SubscriptionPauseStatus.Active => PauseSubscription(),
            SubscriptionPauseStatus.Ended => ResumeSubscription(),
            SubscriptionPauseStatus.Terminated => TerminateSubscriptionPause(),
            SubscriptionPauseStatus.Pending => MaterialisableFlowResult.Empty,
            _ => MaterialisableFlowResult.Empty
        };
    }

    #region private

    private MaterialisableFlowResult PauseSubscription()
    {
        Pause.Status = SubscriptionPauseStatus.Active;

        var pauseDuration = Pause.EndDate - Pause.StartDate;

        Subscription.ExtendEndDate(pauseDuration);

        return Pause.Updated() + Subscription.Updated() + new SubscriptionPausedEvent(Pause, Subscription);
    }

    private MaterialisableFlowResult ResumeSubscription()
    {
        Pause.Status = SubscriptionPauseStatus.Ended;
        Pause.EndDate = UtcNow;

        return Pause.Updated() + new SubscriptionResumedEvent(Pause, Subscription);
    }

    private MaterialisableFlowResult TerminateSubscriptionPause()
    {
        Pause.Status = SubscriptionPauseStatus.Terminated;
        Pause.EndDate = UtcNow;

        return Pause.Updated() + new SubscriptionPausedEvent(Pause, Subscription);
    }

    private bool IsNextStatusValid(SubscriptionPauseStatus currentStatus, SubscriptionPauseStatus? nextStatus)
    {
        if (!nextStatus.HasValue) return false;

        return _transitionMatrix
            .TryGetValue(currentStatus, out var allowedStatuses) && allowedStatuses.Contains(nextStatus.Value);
    }

    private SubscriptionPauseStatus? DetermineNextStatus()
    {
        return Pause.Status switch
        {
            SubscriptionPauseStatus.Pending when CanPauseSubscription() => SubscriptionPauseStatus.Active,
            SubscriptionPauseStatus.Active when NeedResumeSubscription() => SubscriptionPauseStatus.Ended,
            SubscriptionPauseStatus.Active when SubscriptionIsNotActive() => SubscriptionPauseStatus.Terminated,
            SubscriptionPauseStatus.Active when NeedToStayPaused() => SubscriptionPauseStatus.Active,
            SubscriptionPauseStatus.Terminated => null,
            SubscriptionPauseStatus.Ended => null,
            _ => null // current status should not be changed
        };
    }

    private bool SubscriptionIsActive() => Subscription.IsActive;
    
    private bool NeedToStayPaused() => CanPauseSubscription() && !NeedResumeSubscription();
    
    private bool CanPauseSubscription() => Pause.StartDate.Date <= UtcNow.Date && SubscriptionIsActive();

    private bool NeedResumeSubscription() => Pause.EndDate.Date <= UtcNow.Date;

    private bool SubscriptionIsNotActive() => !Subscription.IsActive && Subscription.GetStatus() != SubscriptionStatus.Paused;

    #endregion
}

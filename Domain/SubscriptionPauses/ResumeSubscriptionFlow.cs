using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.Domain.SubscriptionPauses;

public record ResumeSubscriptionFlow(Subscription Subscription, DateTime UtcNow) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var pause = Subscription.Pauses.FirstOrDefault(x => x.IsInRange(UtcNow));

        if (pause is null)
        {
            throw new DomainException("Subscription is not paused");
        }

        pause.EndDate = UtcNow;
        pause.Status = SubscriptionPauseStatus.Ended;

        return pause.Updated() + new SubscriptionResumedEvent(pause, Subscription);
    }
}
using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.Domain.SubscriptionPauses;

public record PauseSubscriptionFlow(
    Subscription Subscription, 
    DateTime EndDate, 
    DateTime UtcNow) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var (startDate, endDate) = AdjustDateRange();

        AssertDateRangeIsValid(startDate, endDate);
       
        AssertSubscriptionIsValid(startDate, endDate);

        var pause = new SubscriptionPause(
            subscription: Subscription,
            startDate: startDate,
            endDate: endDate
        );
        
        return pause.Added();
    }
    
    #region private

    private void AssertSubscriptionIsValid(DateTime startDate, DateTime endDate)
    {
        if (!Subscription.IsActive)
        {
            throw new DomainException("Can't pause inactive subscription");
        }

        if (Subscription.Pauses.Any(x => InRangeWindow(x, startDate, endDate)))
        {
            throw new DomainException("Subscription already paused on this dates");
        }

        if (Subscription.CancellationRequest is not null)
        {
            throw new DomainException("Can't pause subscription with scheduled cancellation");
        }
    }
    
    private void AssertDateRangeIsValid(DateTime startDate, DateTime endDate)
    {
        if (startDate.Date < UtcNow.Date)
        {
            throw new DomainException("Invalid pause date");
        }
            
        if (startDate.Date > EndDate.Date)
        {
            throw new DomainException("Invalid pause date");
        }

        if (endDate.Date < UtcNow.Date)
        {
            throw new DomainException("Invalid pause date");
        }

        if (startDate.Date > Subscription.EndDate.Date)
        {
            throw new DomainException("Invalid pause date");
        }
    }

    private bool InRangeWindow(SubscriptionPause pause, DateTime startDate, DateTime endDate)
    {
        return pause.IsInRange(startDate) || pause.IsInRange(endDate);
    }

    private (DateTime startDate, DateTime endDate) AdjustDateRange()
    {
        var startDate = Subscription.AnchorDate;

        while (UtcNow > startDate)
        {
            startDate = startDate.AddMonths(1);
        }
        
        return (startDate, EndDate);
    }

    #endregion
}
using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Infrastructure.Data.Queries;
using static WildHealth.Domain.Enums.Payments.PaymentIssueStatus;

namespace WildHealth.Application.Domain.PaymentIssues;

public static class PaymentIssueExtensions
{
    /// <summary>
    /// Get the last active payment issue
    /// </summary>
    public static PaymentIssue? LastActive(this IEnumerable<PaymentIssue> source)
    {
        return source
            .AsQueryable()
            .Active()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
    }
    
    public static MaterialisableFlowResult UserCancel(this PaymentIssue source)
    {
        var cancelableStatuses = new[] { WaitingPatientNotification, PatientNotified, CareCoordinatorNotified };
        return cancelableStatuses.Contains(source.Status) ? 
            new PaymentIssueStatusChanged(source.GetId(), new PaymentIssueStatusChangedData(UserCancelled)).ToFlowResult() : 
            MaterialisableFlowResult.Empty;
    }
    
    public static int DaysOverdue(this PaymentIssue source, DateTime now) => 
        (now - source.CreatedAt).Days;
    
    public static Subscription CurrentSubscription(this IEnumerable<SubscriptionIntegration> source)
    {
        return source
            .OrderByDescending(x => x.Subscription.StartDate)
            .Select(x => x.Subscription)
            .First();
    }
}
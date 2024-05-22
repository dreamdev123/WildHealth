using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.PatientTasks;
using WildHealth.Domain.Entities.Engagement;
using static WildHealth.Domain.Entities.Engagement.PatientEngagementStatus;

namespace WildHealth.Application.Domain.PatientEngagements.Flows;

public record PatientTaskQueryFlow(IQueryable<PatientEngagement> Source, int PatientId, DateTime Timestamp) : IQueryFlow<PatientEngagementTaskModel>
{
    public IQueryable<PatientEngagementTaskModel> Execute()
    {
        var filtered = Source
            .Where(x => 
                x.PatientId == PatientId && 
                !x.IsPremium &&
                x.ExpirationDate.Date >= Timestamp.Date && // not expired
                (x.EngagementCriteria.Assignee & EngagementAssignee.Patient) == EngagementAssignee.Patient &&
                x.EngagementCriteria.NotificationType != EngagementNotificationType.Dashboard &&
                (x.Status == InProgress || x.Status == PendingAction) &&
                !x.EngagementCriteria.IsDisabled);

        return filtered.Select(x => new PatientEngagementTaskModel
        {
            DisplayName = x.EngagementCriteria.DisplayName,
            Type = x.EngagementCriteria.Type,
            AnalyticsEvent = x.EngagementCriteria.AnalyticsEvent,
            Reason = x.EngagementCriteria.Reason
        });
    }
}
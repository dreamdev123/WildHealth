using System;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Notifications.Abstracts;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using static WildHealth.Domain.Entities.Engagement.EngagementNotificationType;

namespace WildHealth.Application.Domain.PatientEngagements.Flows;

public record NotifyPatientsInNeedOfEngagementFlow(PatientEngagement PatientEngagement, 
    string DashboardUrl,
    DateTime UtcNow) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (PatientEngagement.Status != PatientEngagementStatus.PendingAction || PatientEngagement.Expired(UtcNow)) return MaterialisableFlowResult.Empty;
        
        PatientEngagement.Status = PatientEngagementStatus.InProgress;
        
        return PatientEngagement.Updated() + BuildNotifications();
    }

    private IEnumerable<IBaseNotification> BuildNotifications()
    {
        if ((PatientEngagement.EngagementCriteria.NotificationType & SMS) == SMS)
            yield return PatientEngagementSmsNotification.Create(PatientEngagement, DashboardUrl, UtcNow);
        
        if ((PatientEngagement.EngagementCriteria.NotificationType & Email) == Email)
            yield return PatientEngagementEmailNotification.Create(PatientEngagement, DashboardUrl, UtcNow);
    }
}
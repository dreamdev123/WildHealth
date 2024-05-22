using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;

namespace WildHealth.Application.Domain.PatientEngagements.Flows;

public record SendTestStaffEmailNotificationFlow() : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        return MaterialisableFlowResult.Empty + new TestStaffEmailNotification();  
    }
}
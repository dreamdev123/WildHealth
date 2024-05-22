using OneOf.Monads;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.Domain.PatientEngagements.Flows;

public record RaiseEngagementTaskCountForTodayFlow(Option<UserSetting> UserSetting, int UserId, int CurrentActiveTaskCount) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var delta = EngagementTasksCount.Default.Count - CurrentActiveTaskCount;

        if (UserSetting.HasValue())
        {
            var entity = UserSetting.Value();
            entity.Value = (int.Parse(entity.Value) + delta).ToString();
            return entity.Updated();
        }

        return new UserSetting
        {
            Key = EngagementTasksCount.Key,
            UserId = UserId,
            Value = (EngagementTasksCount.Default.Count + delta).ToString()
        }.Added();
    }
}
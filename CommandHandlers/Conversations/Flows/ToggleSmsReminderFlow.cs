using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.CommandHandlers.Conversations.Flows;

public record ToggleSmsReminderFlow(Patient Patient, bool IsActive): IMaterialisableFlow
{
    private const string SettingsKey = "UnreadMessagesSmsReminder";
    
    public MaterialisableFlowResult Execute()
    {
        var settings = Patient.User.Settings.FirstOrDefault(x => x.Key == SettingsKey);

        if (settings is null)
        {
            return new UserSetting
            {
                Key = SettingsKey,
                Value = IsActive.ToString(),
                UserId = Patient.UserId
            }.Added();
        }

        settings.Value = IsActive.ToString();
        return settings.Updated();
    }
}
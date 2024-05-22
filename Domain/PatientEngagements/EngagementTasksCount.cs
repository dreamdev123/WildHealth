namespace WildHealth.Application.Domain.PatientEngagements;

public record EngagementTasksCount(int Count)
{
    public static string Key => "HealthCoachEngagementCount";
    public static EngagementTasksCount Default => new(5);
}
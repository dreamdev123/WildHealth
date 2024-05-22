using System;
using WildHealth.ClarityCore.Models.Insights;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.Vitals;

namespace WildHealth.Application.Extensions.Inputs;

public static class InsightExtensions
{
    public static Insight ToInsight(this InsightModel model)
    {
        return new Insight(
            id: model.Id,
            name: model.ToVitalName(),
            displayName: model.ToVitalDisplayName(),
            value: decimal.TryParse(model.Value, out var t) ? t : 0,
            date: new DateTime(model.CollectionDate.Year, model.CollectionDate.Month, model.CollectionDate.Day),
            dimension: model.Units,
            sourceType: VitalValueSourceType.MobileApplication
        );
    }
    public static string ToVitalName(this InsightModel insight)
    {
        return insight.MarkerName switch
        {
            "Weight" => VitalNames.Weight.Name,
            "Calories Burned" => VitalNames.Calories.Name,
            "Total Calories Burned" => VitalNames.TotalCalories.Name,
            "Resting Heart Rate" => VitalNames.HeartRate.Name,
            "Steps" => VitalNames.Steps.Name,
            _ => string.Empty
        };
        
    }
    
    public static string ToVitalDisplayName(this InsightModel insight)
    {
        return insight.MarkerName switch
        {
            "Weight" => VitalNames.Weight.DisplayName,
            "Calories Burned" => VitalNames.Calories.DisplayName,
            "Total Calories Burned" => VitalNames.TotalCalories.DisplayName,
            "Resting Heart Rate" => VitalNames.HeartRate.DisplayName,
            "Steps" => VitalNames.Steps.DisplayName,
            _ => string.Empty
        };
    }
}
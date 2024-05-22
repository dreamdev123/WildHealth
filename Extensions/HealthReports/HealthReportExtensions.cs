using WildHealth.Common.Models.Reports._Base;
using WildHealth.Domain.Entities.Reports._Base;

namespace WildHealth.Application.Extensions.HealthReports
{
    public static class HealthReportExtensions
    {
        public static void MapRecommendations(
            this ReportRecommendationBase recommendation,
            ReportRecommendationBaseModel model)
        {
            recommendation.Content = model.Content;
            recommendation.ManualContent = model.ManualContent;
        }
        
        public static void MapRecommendations(
            this ReportRecommendationBase recommendation,
            ReportRecommendationBase baseRecommendation)
        {
            recommendation.Content = baseRecommendation.Content;
            recommendation.ManualContent = baseRecommendation.ManualContent;
        }
    }
}
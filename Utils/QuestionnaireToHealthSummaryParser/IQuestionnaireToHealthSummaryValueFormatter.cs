using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Domain.Entities.Questionnaires;

namespace WildHealth.Application.Utils.QuestionnaireToHealthSummaryParser;

public interface IQuestionnaireToHealthSummaryValueFormatter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mapItem"></param>
    /// <param name="value"></param>
    /// <param name="questions"></param>
    /// <returns></returns>
    string FormatValue(HealthSummaryMapItem mapItem, string value, Question[] questions);
}
using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Domain.Entities.Questionnaires;

namespace WildHealth.Application.Utils.QuestionnaireToHealthSummaryParser;

public class QuestionnaireToHealthSummaryValueFormatter : IQuestionnaireToHealthSummaryValueFormatter
{
    /// <summary>
    /// <see cref="IQuestionnaireToHealthSummaryValueFormatter.FormatValue"/>
    /// </summary>
    /// <param name="mapItem"></param>
    /// <param name="value"></param>
    /// <param name="questions"></param>
    /// <returns></returns>
    public string FormatValue(HealthSummaryMapItem mapItem, string value, Question[] questions)
    {
        var key = ParseKey(mapItem.Key);
        var parser = key is not null && Parsers.ContainsKey(key)
            ? Parsers[key]
            : Parsers["DEFAULT"];
        
        return parser.Invoke(mapItem, value, questions);
    }
    
    private static string? ParseKey(string key)
    {
        return key.Split('-').FirstOrDefault();
    }
    
    private static readonly IDictionary<string, Func<HealthSummaryMapItem, string, Question[], string>> Parsers =
        new Dictionary<string, Func<HealthSummaryMapItem, string, Question[], string>>
        {
            {
                CancerIssuesVariants,
                (mapItem, value, questions) =>
                {
                    var question = questions.FirstOrDefault(x => x.Name == CancerIssuesVariants);
                    return question != null && question.Variants.Contains(value) 
                        ? mapItem.ViewLabelCondition.Replace("[value]", value) 
                        : value;
                }
            },
            {
                Default,
                (mapItem, value, _) => string.IsNullOrEmpty(mapItem.ViewLabelCondition)
                    ? value
                    : mapItem.ViewLabelCondition.Replace("[value]", value)
            },
        };

    private const string CancerIssuesVariants = "CANCER_ISSUES_VARIANTS";
    private const string Default = "DEFAULT";
}
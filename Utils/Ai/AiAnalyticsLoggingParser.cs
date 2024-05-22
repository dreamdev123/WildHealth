using System;
using System.Linq;
using WildHealth.Common.Models.Ai;

namespace WildHealth.Application.Utils.Ai;

public class AiAnalyticsLoggingParser : IAiAnalyticsLoggingParser
{
    public T? GetValueOrDefaultForKey<T>(AiAnalyticsLoggingModel model, string key)
    {
        var result = model.AnalyticsLogInformation.FirstOrDefault(x => x.Key == key)?.Value;

        if (result == null)
        {
            return default;
        }
        
        return (T)Convert.ChangeType(result, typeof(T));
    }
}
using WildHealth.Common.Models.Ai;

namespace WildHealth.Application.Utils.Ai;

public interface IAiAnalyticsLoggingParser
{ T? GetValueOrDefaultForKey<T>(AiAnalyticsLoggingModel model, string key);
}
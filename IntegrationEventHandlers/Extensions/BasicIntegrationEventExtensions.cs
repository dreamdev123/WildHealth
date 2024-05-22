using Newtonsoft.Json;
using WildHealth.IntegrationEvents._Base;

namespace WildHealth.Application.IntegrationEventHandlers.Extensions;

public static class BasicIntegrationEventExtensions
{
    public static T DeserializePayload<T>(this BaseIntegrationEvent @event) =>
        JsonConvert.DeserializeObject<T>(@event.Payload.ToString());
}
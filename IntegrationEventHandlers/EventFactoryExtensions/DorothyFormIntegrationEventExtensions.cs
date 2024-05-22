using System.Linq;
using AutoMapper;
using Newtonsoft.Json;
using WildHealth.Application.Events.Scheduler;
using WildHealth.IntegrationEvents.FormIntegrations;
using WildHealth.IntegrationEvents.FormIntegrations.Payloads;
using WildHealth.IntegrationEvents.Orders.Payloads;
using WildHealth.IntegrationEvents.Scheduler.Payloads;
using WildHealth.TimeKit.Clients.Models.Customers;

namespace WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;

public static class DorothyFormIntegrationEventExtensions
{
    public static T ToSyncRecordType<T>(this DorothyFormSubmittedPayload source, IMapper mapper) =>
        mapper.Map<T>(source);
    
    public static T DeserializeIntegrationEventPayload<T>(this DorothyFormIntegrationEvent @event) =>
        JsonConvert.DeserializeObject<T>(@event.Payload.ToString());
}
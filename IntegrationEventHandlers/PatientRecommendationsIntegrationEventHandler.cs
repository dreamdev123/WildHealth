using System;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Recommendations;
using WildHealth.IntegrationEvents.Recommendations.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers;

public class PatientRecommendationsIntegrationEventHandler : IEventHandler<PatientRecommendationsIntegrationEvent>
{
    private readonly IMediator _mediator;
    
    public PatientRecommendationsIntegrationEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(PatientRecommendationsIntegrationEvent @event)
    {
        var notification = CreateNotification(@event);
        
        await _mediator.Publish(notification);
    }
    
    private INotification CreateNotification(PatientRecommendationsIntegrationEvent @event)
    {
        return @event.PayloadType switch
        {
            nameof(PatientMetricsUpdatedPayload) =>  (@event.Payload as PatientMetricsUpdatedPayload ?? @event.DeserializePayload<PatientMetricsUpdatedPayload>())
                .ToPatientMetricsUpdatedEvent(),
            nameof(PatientMetricSourcesUpdatedPayload) => (@event.Payload as PatientMetricSourcesUpdatedPayload ?? @event.DeserializePayload<PatientMetricSourcesUpdatedPayload>())
                .ToPatientMetricSourcesUpdatedEvent(),
            _ => throw new ArgumentOutOfRangeException($"Handler for PatientRecommendationsIntegrationEvent event with [Type] = {@event.PayloadType} is not implemented")
        };
    }
}
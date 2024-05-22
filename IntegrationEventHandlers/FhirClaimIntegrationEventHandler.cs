using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Events.Insurances;
using WildHealth.Domain.Constants;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers;

public class FhirClaimIntegrationEventHandler : IEventHandler<FhirClaimIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly  ILogger<FhirClaimIntegrationEventHandler> _logger;

    public FhirClaimIntegrationEventHandler(
        IMediator mediator,
        ILogger<FhirClaimIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(FhirClaimIntegrationEvent @event)
    {
        _logger.LogInformation($"Started processing claim integration event {@event.Id} with payload type: {@event.PayloadType} - {@event.Payload}");

        try
        {
            var notification = CreateNotification(@event);

            await _mediator.Publish(notification);
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed processing claim integration event {@event.Id} with payload: {@event.Payload}. {e}");
            throw;
        }
        
        _logger.LogInformation($"Processed claim integration event {@event.Id} with payload: {@event.Payload}");
    }
    
    #region private

    private INotification CreateNotification(FhirClaimIntegrationEvent @event)
    {
        switch (@event.PayloadType)
        {
            case nameof(FhirClaimSubmittedPayload):
                var claimSubmittedPayload = @event.DeserializePayload<FhirClaimSubmittedPayload>();
                return new ClaimSubmittedEvent(
                    claimId: claimSubmittedPayload.Id,
                    practiceId: claimSubmittedPayload.PracticeId,
                    date: claimSubmittedPayload.Date);
            case nameof(FhirClaimDeniedPayload):
                var claimDeniedPayload = @event.DeserializePayload<FhirClaimDeniedPayload>();
                return new ClaimDeniedEvent(
                    claimId: claimDeniedPayload.Id, 
                    practiceId: claimDeniedPayload.PracticeId,
                    date: claimDeniedPayload.Date);
            case nameof(FhirClaimPaidPayload):
                var claimPaidPayload = @event.DeserializePayload<FhirClaimPaidPayload>();
                return new ClaimPaidEvent(
                    claimId: claimPaidPayload.Id,
                    practiceId: claimPaidPayload.PracticeId,
                    date: claimPaidPayload.Date);
            case nameof(FhirClaimStatusChangedPayload):
                var claimStatusChangedPayload = @event.DeserializePayload<FhirClaimStatusChangedPayload>();
                return new ClaimStatusChangedEvent(
                    claimId: claimStatusChangedPayload.Id,
                    practiceId: claimStatusChangedPayload.PracticeId,
                    date: claimStatusChangedPayload.Date,
                    newStatusId: claimStatusChangedPayload.NewStatusId,
                    status: claimStatusChangedPayload.Entity,
                    statusCode: claimStatusChangedPayload.StatusCode,
                    category: claimStatusChangedPayload.Category);
                
            default:
                throw new ArgumentOutOfRangeException(
                    $"Handler for claim integration event with [Type] = {@event.PayloadType} is not implemented");
        }
    }

    #endregion
}
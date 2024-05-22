using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers;

public class FhirRemitIntegrationEventHandler : IEventHandler<FhirRemitIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly  ILogger<FhirRemitIntegrationEventHandler> _logger;
    private readonly IOptions<PracticeOptions> _options;

    public FhirRemitIntegrationEventHandler(
        IMediator mediator,
        ILogger<FhirRemitIntegrationEventHandler> logger,
        IOptions<PracticeOptions> options)
    {
        _mediator = mediator;
        _logger = logger;
        _options = options;
    }

    public async Task Handle(FhirRemitIntegrationEvent @event)
    {
        _logger.LogInformation($"Started processing fhir remit integration event {@event.Id} with payload type: {@event.PayloadType} - {@event.Payload}");

        try
        {
            switch (@event.PayloadType)
            {
                case nameof(FhirRemitFileProcessedPayload):
                    await ProcessRemitFileProcessedPayload(JsonConvert.DeserializeObject<FhirRemitFileProcessedPayload>(@event.Payload.ToString()));
                    break;

                default: throw new ArgumentException("Unsupported fhir remitmintegration event payload");
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed processing fhir remit integration event {@event.Id} with payload: {@event.Payload}. {e}");
            throw;
        }
        
        _logger.LogInformation($"Processed fhir remit integration event {@event.Id} with payload: {@event.Payload}");
    }
    
    #region private

    /// <summary>
    /// Processes claim updated payload
    /// </summary>
    /// <param name="payload"></param>
    private async Task ProcessRemitFileProcessedPayload(FhirRemitFileProcessedPayload payload)
    {
        if (payload.PracticeId == _options.Value.MurrayMedicalId)
        {
            var command = new HandleRemitFileProcessedCommand(Convert.ToInt32(payload.Id));

            await _mediator.Send(command);
        }
    }
    
    #endregion
}
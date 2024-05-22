using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers;

public class FhirChargeIntegrationEventHandler : IEventHandler<FhirChargeIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly  ILogger<FhirChargeIntegrationEventHandler> _logger;

    public FhirChargeIntegrationEventHandler(
        IMediator mediator,
        ILogger<FhirChargeIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(FhirChargeIntegrationEvent @event)
    {
        _logger.LogInformation($"Started processing fhir charge integration event {@event.Id} with payload type: {@event.PayloadType} - {@event.Payload}");

        try
        {
            switch (@event.PayloadType)
            {
                case nameof(FhirChargeCreatedPayload):
                    await ProcessFhirChargeCreatedPayload(JsonConvert.DeserializeObject<FhirChargeCreatedPayload>(@event.Payload.ToString()));
                    break;
                
                case nameof(FhirChargeDeniedPayload):
                    await ProcessFhirChargeDeniedPayload(JsonConvert.DeserializeObject<FhirChargeDeniedPayload>(@event.Payload.ToString()));
                    break;

                default: throw new ArgumentException("Unsupported fhir charge integration event payload");
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed processing fhir charge integration event {@event.Id} with payload: {@event.Payload}. {e}");
            throw;
        }
        
        _logger.LogInformation($"Processed fhir charge integration event {@event.Id} with payload: {@event.Payload}");
    }
    
    #region private
    
    private async Task ProcessFhirChargeCreatedPayload(FhirChargeCreatedPayload payload)
    {
        var command = new SendDorothyChargeSubmittedCommsCommand(payload.Id, IntegrationVendor.OpenPm);

        await _mediator.Send(command);
    }
    
    private async Task ProcessFhirChargeDeniedPayload(FhirChargeDeniedPayload payload)
    {
        var command = new SendDorothyChargeDeniedCommsCommand(payload.Id, IntegrationVendor.OpenPm);

        await _mediator.Send(command);
    }
    
    #endregion
}
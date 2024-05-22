using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.EpigeneticOrders;
using WildHealth.IntegrationEvents.EpigeneticOrders.Payloads;
using WildHealth.IntegrationEvents.Fhir.Payloads;
using Newtonsoft.Json;
using MediatR;
using WildHealth.IntegrationEvents._Base;

namespace WildHealth.Application.IntegrationEventHandlers;

public class EpigeneticOrderIntegrationEventHandler : IEventHandler<EpigeneticOrderIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly  ILogger _logger;

    public EpigeneticOrderIntegrationEventHandler(
        IMediator mediator,
        ILogger<EpigeneticOrderIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    public async Task Handle(EpigeneticOrderIntegrationEvent @event)
    {
        _logger.LogInformation($"Started processing epigenetic order integration event {@event.Id} with payload type: {@event.PayloadType} - {@event.Payload}");

        try
        {
            switch (@event.PayloadType)
            {
                case nameof(EpigeneticOrderResultedPayload):
                    await ProcessEpigeneticOrderResultedPayload(JsonConvert.DeserializeObject<EpigeneticOrderResultedPayload>(@event.Payload.ToString()), @event.Patient);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed processing epigenetic order integration event {@event.Id} with payload: {@event.Payload}. {e}");
            throw;
        }
        
        _logger.LogInformation($"Processed epigenetic order integration event {@event.Id} with payload: {@event.Payload}");
    }
    
    #region private

    private Task ProcessEpigeneticOrderResultedPayload(EpigeneticOrderResultedPayload payload, PatientMetadataModel patient)
    {
        var command = new ProcessEpigeneticOrderResultsCommand(
            patientId: patient.Id,
            orderId: payload.OrderId,
            orderNumber: payload.OrderNumber
        );

        return _mediator.Send(command);
    }
    
    #endregion
}
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.DnaFiles;
using WildHealth.IntegrationEvents.DnaFiles.Payloads;
using MediatR;
using Newtonsoft.Json;

namespace WildHealth.Application.EventHandlers.Inputs;

public class SynchronizeDnaFileEventHandler : IEventHandler<DnaFileIntegrationEvent>
{
    private readonly ILogger<SynchronizeDnaFileEventHandler> _logger;
    private readonly IMediator _mediator;

    public SynchronizeDnaFileEventHandler(
        ILogger<SynchronizeDnaFileEventHandler> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }
    
    public async Task Handle(DnaFileIntegrationEvent @event)
    {
        _logger.LogInformation($"Started processing order integration event {@event.Id}");

        try
        {
            switch (@event.PayloadType)
            {
                case nameof(DnaFileCompletedPayload): 
                    await ProcessCompletedDnaFilePayload(JsonConvert.DeserializeObject<DnaFileCompletedPayload>(@event.Payload.ToString())); 
                    break;
                
                case nameof(DnaFileFailedPayload): 
                    await ProcessFailedDnaFilePayload(JsonConvert.DeserializeObject<DnaFileFailedPayload>(@event.Payload.ToString())); 
                    break;

                default: throw new ArgumentException("Unsupported order integration event payload");
            }

            _logger.LogInformation($"Processed order integration event {@event.Id}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed processing order integration event {@event.Id}", e);
            throw;
        }
    }

    private async Task ProcessCompletedDnaFilePayload(DnaFileCompletedPayload payload)
    {
        var command = new SynchronizeCompletedDnaFileCommand(payload.FileName);
        
        await _mediator.Send(command);
    }
    
    private async Task ProcessFailedDnaFilePayload(DnaFileFailedPayload payload)
    {
        var command = new SynchronizeFailedDnaFileCommand(payload.FileName);
        
        await _mediator.Send(command);
    }
}
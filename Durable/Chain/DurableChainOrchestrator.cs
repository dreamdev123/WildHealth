using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.Messages;
using WildHealth.IntegrationEvents.Messages;
using WildHealth.IntegrationEvents.Messages.Payloads;


namespace WildHealth.Application.Durable.Chain;

public class DurableChainOrchestrator : IDurableChainOrchestrator
{
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly IWebHostEnvironment _hostEnvironment;

    public DurableChainOrchestrator(
        IEventBus eventBus,
        ILogger<DurableChainOrchestrator> logger, 
        IWebHostEnvironment hostEnvironment)
    {
        _eventBus = eventBus;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    /// <inheritdoc/>
    public async Task<TPayload?> Run<TPayload>(TPayload? payload,
        string startAtStep,
        Action<ChainOfResponsibility<TPayload>> chainBuilder) where TPayload : IEvent
    {
        var chain = new ChainOfResponsibility<TPayload>(startAtStep, payload!, _logger.LogInformation);
        chainBuilder.Invoke(chain);
        
        try
        {
            _logger.LogInformation("Starting chain execution at step of '{StartStep}'. {Payload}", startAtStep, payload);
            var result = await chain.Run();
            _logger.LogInformation("Chain execution finished. {Result}", result);

            return result;
        }
        catch (ChainStateChangedException e)
        {
            _logger.LogError(
                e,
                "Something went wrong at step of '{Step}'. {Payload}. Re-publishing the event to retry.", 
                e.StepName, 
                e.Payload);
            
            // publish the new event to retry at that point where it failed 
            await _eventBus.Publish(e.Payload as IEvent);
            
            // return default result and do not propagate the exception further let the message be acknowledged 
            return default;
        }
        catch (Exception e)
        {
            _logger.LogError(
                exception: e,
                message: "Couldn't process the event at step of '{Step}'. {Payload}.", 
                startAtStep, 
                payload);

            var errorMessage = $"Environment: {_hostEnvironment.EnvironmentName} " +
                $"Something went wrong when processing '{typeof(TPayload).FullName}'. " +
                $"Re-submit the message from the dead letter queue once the root cause is resolved. " +
                $"Payload: '{JsonConvert.SerializeObject(payload)}'; Exception: {e}";
            
            var errorMessageEvent = new SlackMessagePayload(
                message: errorMessage,
                messageType: "BackgroundProcessCrashed"
            );
            
            // notifying the system about the chain crash so it's acted upon
            await _eventBus.Publish(new MessageIntegrationEvent(errorMessageEvent, DateTime.UtcNow));

            throw;
        }
    }
}
using System;
using System.Threading.Tasks;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Shared.Data.Context;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace WildHealth.Application.EventHandlers.Dummy;

public class DummyIntegrationEventHandler : IEventHandler<DummyIntegrationEvent>
{
    private ILogger<DummyIntegrationEventHandler> _logger;
    private IApplicationDbContext _dbContext;
    public DummyIntegrationEventHandler(ILogger<DummyIntegrationEventHandler> logger,
                                        IApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
        _logger.LogInformation("Creating DummyIntegrationEventHandler.");
    }

    public Task Handle(DummyIntegrationEvent @event)
    {
        var threadId = Thread.CurrentThread.ManagedThreadId;
        var message = $"Thread {threadId} sleeping for {@event.SleepSeconds} seconds.";
        _logger.LogInformation(message);
        try
        {
            //The default hashcode implementation is good for reference comparison.
            var reference = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_dbContext);
            _logger.LogInformation($"dbcontext reference: {reference}");
            _logger.LogInformation($"this reference: {this.GetHashCode()}");
            Thread.Sleep(@event.SleepSeconds * 1000);
            _logger.LogInformation($"Thread {threadId} done.");
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
        }
        return Task.CompletedTask;
    }
}

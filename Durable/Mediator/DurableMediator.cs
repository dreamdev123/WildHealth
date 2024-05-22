using System;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;

namespace WildHealth.Application.Durable.Mediator;

public class DurableMediator : IDurableMediator
{
    private readonly IEventBus _eventBus;

    public DurableMediator(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Publish<T>(T @event) where T : INotification
    {
        var durableCommand = new DurableMediatorEvent(@event, DateTime.Now);
        await _eventBus.Publish(durableCommand);
    }
}
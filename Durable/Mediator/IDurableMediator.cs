using System.Threading.Tasks;
using MediatR;

namespace WildHealth.Application.Durable.Mediator;

public interface IDurableMediator
{
    /// <summary>
    /// Asynchronously send a notification to multiple MediatR handlers via ServiceBus.
    /// NOTE: The event must be serializable and should not have private setters. 
    /// </summary>
    Task Publish<T>(T @event) where T : INotification;
}
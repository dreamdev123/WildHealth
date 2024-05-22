using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Application.EventSourcing;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Services.Notifications;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Data.EntityStateManager;

namespace WildHealth.Application.Materialization;

public delegate Task MaterializeFlow(MaterialisableFlowResult source);

public class FlowMaterialization : IFlowMaterialization
{
    private readonly IEntityStateManager _entityStateManager;
    private readonly INotificationService _notificationService;
    private readonly IEventBus _eventBus;
    private readonly IMediator _mediator;
    private readonly IDurableMediator _durableMediator;
    
    public FlowMaterialization(
        IEntityStateManager entityStateManager, 
        IEventBus eventBus, 
        INotificationService notificationService, 
        IMediator mediator, 
        IDurableMediator durableMediator)
    {
        _entityStateManager = entityStateManager;
        _eventBus = eventBus;
        _notificationService = notificationService;
        _mediator = mediator;
        _durableMediator = durableMediator;
    }

    public async Task Materialize(MaterialisableFlowResult source)
    {
        var entityActions = source.EntityActions.ToArray();
        foreach (var action in entityActions)
        {
            switch (action)
            {
                case EntityAction.None or { Entity: null } or null:
                    // do nothing
                    break;
                case EntityAction.Add add:
                    await _entityStateManager.AddAsync(add.Entity);
                    break;
                case EntityAction.Update update:
                    _entityStateManager.Edit(update.Entity);
                    break;
                case EntityAction.Delete delete:
                    _entityStateManager.Delete(delete.Entity);
                    break;
                default:
                    throw new IndexOutOfRangeException($"Unknown entity action: {action.GetType().Name}");
            }
        }

        var aggregateEvents = source.AggregateEvents.Select(e => e.ToJournalEvent()).ToArray();
        foreach (var aggregateEvent in aggregateEvents) 
            await _entityStateManager.AddAsync(aggregateEvent);
        
        if (entityActions.Any() || aggregateEvents.Any())
            await _entityStateManager.SaveChanges();

        foreach (var aggregateEvent in aggregateEvents)
            await _durableMediator.Publish(new EventJournalCreated(aggregateEvent.GetId()));

        foreach (var e in source.MediatorEvents)
            await _mediator.Publish(e);
        
        await Task.WhenAll(source.IntegrationEvents.Select(e => _eventBus.Publish(e)));

        // can't do in parallel because possible database calls but DbContext is not thread safe 
        foreach (var notification in source.Notifications) 
            await _notificationService.CreateNotificationAsync(notification);
    } 
}
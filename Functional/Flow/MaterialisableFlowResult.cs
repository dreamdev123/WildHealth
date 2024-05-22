using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using WildHealth.Domain.Entities.Notifications.Abstracts;
using WildHealth.Domain.Models.Extensions;
using WildHealth.EventSourcing;
using WildHealth.IntegrationEvents._Base;

namespace WildHealth.Application.Functional.Flow;

public record MaterialisableFlowResult(
    IEnumerable<EntityAction> EntityActions,
    IEnumerable<BaseIntegrationEvent> IntegrationEvents, 
    IEnumerable<IBaseNotification> Notifications)
{
    public MaterialisableFlowResult(EntityAction entityAction) 
        : this(entityAction.ToEnumerable()) { }
    
    public MaterialisableFlowResult(IEnumerable<EntityAction> EntityActions) 
        : this(EntityActions, Enumerable.Empty<BaseIntegrationEvent>(), Enumerable.Empty<IBaseNotification>()) { }
    
    public MaterialisableFlowResult(IEnumerable<EntityAction> EntityActions, IEnumerable<BaseIntegrationEvent> IntegrationEvents) 
        : this(EntityActions, IntegrationEvents, Enumerable.Empty<IBaseNotification>()) { }
    
    public MaterialisableFlowResult(IEnumerable<EntityAction> EntityActions, IEnumerable<IBaseNotification> Notifications) 
        : this(EntityActions,Enumerable.Empty<BaseIntegrationEvent>(), Notifications) { }

    public IEnumerable<EntityAction> EntityActions { get; } = EntityActions.Where(x => x is not EntityAction.None);
    
    public static MaterialisableFlowResult Empty { get; } = new(Enumerable.Empty<EntityAction>());

    public IEnumerable<INotification> MediatorEvents { get; set; } = Array.Empty<INotification>();
    

    public IEnumerable<IAggregateEvent> AggregateEvents { get; set; } = Array.Empty<IAggregateEvent>();

    public static MaterialisableFlowResult operator +(MaterialisableFlowResult a, MaterialisableFlowResult b) => a.Concat(b);
    public static MaterialisableFlowResult operator +(MaterialisableFlowResult a, INotification b) => a.Concat(b.ToFlowResult());
    public static MaterialisableFlowResult operator +(MaterialisableFlowResult a, IEnumerable<INotification> b) => a.Concat(b.ToFlowResult());
    public static MaterialisableFlowResult operator +(MaterialisableFlowResult a, EntityAction b) => a.Concat(b.ToFlowResult());
    public static MaterialisableFlowResult operator +(MaterialisableFlowResult a, IEnumerable<EntityAction> b) => a.Concat(b.ToFlowResult());
    public static MaterialisableFlowResult operator +(MaterialisableFlowResult a, BaseIntegrationEvent b) => a.Concat(b.ToFlowResult());
    public static MaterialisableFlowResult operator +(MaterialisableFlowResult a, IEnumerable<BaseIntegrationEvent> b) => a.Concat(b.ToFlowResult());
    public static MaterialisableFlowResult operator +(MaterialisableFlowResult a, IBaseNotification b) => a.Concat(b.ToFlowResult());
    public static MaterialisableFlowResult operator +(MaterialisableFlowResult a, IAggregateEvent b) => a.Concat(b.ToFlowResult());
    public static MaterialisableFlowResult operator +(MaterialisableFlowResult a, IEnumerable<IAggregateEvent> b) => a.Concat(b.ToFlowResult());
}
using System;
using System.Collections.Generic;
using MediatR;
using WildHealth.Domain.Entities.Notifications.Abstracts;
using WildHealth.EventSourcing;
using WildHealth.IntegrationEvents._Base;
using WildHealth.Shared.Data.Entities;

namespace WildHealth.Application.Functional.Flow;

public abstract record EntityAction(BaseEntity Entity)
{
    private EntityAction() : this(new NoneEntity()) { }

    public record Add(BaseEntity Entity) : EntityAction(Entity);

    public record Update(BaseEntity Entity) : EntityAction(Entity);

    public record Delete(BaseEntity Entity) : EntityAction(Entity);

    public record None : EntityAction
    {
        public new BaseEntity Entity => throw new NullReferenceException("None entity doesn't have any value");
        public static EntityAction Instance => new None();
        public override bool HasValue => false;
    }

    public virtual bool HasValue => Entity is not null;
    
    public static MaterialisableFlowResult operator +(EntityAction a, EntityAction b) => new(EntityActions: new List<EntityAction> { a, b });
    public static MaterialisableFlowResult operator +(EntityAction a, INotification b) => a.ToFlowResult() + b.ToFlowResult();
    public static MaterialisableFlowResult operator +(EntityAction a, IBaseNotification b) => a.ToFlowResult() + b.ToFlowResult();
    public static MaterialisableFlowResult operator +(EntityAction a, BaseIntegrationEvent b) => a.ToFlowResult() + b.ToFlowResult();
    public static MaterialisableFlowResult operator +(EntityAction a, IAggregateEvent b) => a.ToFlowResult() + b.ToFlowResult();
    public static MaterialisableFlowResult operator +(EntityAction a, MaterialisableFlowResult b) => a.ToFlowResult() + b;
    public static MaterialisableFlowResult operator +(EntityAction a, IEnumerable<EntityAction> b) => a + b.ToFlowResult();
    public static MaterialisableFlowResult operator +(EntityAction a, IEnumerable<INotification> b) => a.ToFlowResult() + b.ToFlowResult();
    public static MaterialisableFlowResult operator +(EntityAction a, IEnumerable<IBaseNotification> b) => a.ToFlowResult() + b.ToFlowResult();
    public static MaterialisableFlowResult operator +(EntityAction a, IEnumerable<BaseIntegrationEvent> b) => a.ToFlowResult() + b.ToFlowResult();
    public static implicit operator MaterialisableFlowResult(EntityAction e) => e.ToFlowResult();
    
    private class NoneEntity : BaseEntity {}
}

public static class EntityActionExtensions 
{
    public static EntityAction Added(this BaseEntity? source) => source is not null ? new EntityAction.Add(source) : new EntityAction.None();
    public static EntityAction Updated(this BaseEntity? source) => source is not null ? new EntityAction.Update(source) : new EntityAction.None();
    public static EntityAction Deleted(this BaseEntity? source) => source is not null ? new EntityAction.Delete(source) : new EntityAction.None();
    
    public static IEnumerable<EntityAction> Concat(this EntityAction left, params EntityAction[] actions)
    {
        yield return left;
        foreach (var action in actions)
            yield return action;
    }
    
    public static TEntity Entity<TEntity>(this EntityAction source) where TEntity : BaseEntity
    {
        if (source.Entity is TEntity entity)
            return entity;

        throw new InvalidCastException($"Can't cast {source.Entity.GetType().Name} to {typeof(TEntity).Name}");
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Entities.Notifications.Abstracts;
using WildHealth.Domain.Models;
using WildHealth.Domain.Models.Extensions;
using WildHealth.EventSourcing;
using WildHealth.IntegrationEvents._Base;
using WildHealth.Shared.Data.Entities;

namespace WildHealth.Application.Functional.Flow;

public static class FlowExtensions
{
    public static async Task<TResult> Materialize<TResult>(
        this IFlow<TResult> flow,
        MaterializeFlow materialize) where TResult : MaterialisableFlowResult
    {
        var result = flow.Execute();
        await materialize.Invoke(result);
        return result;
    }
    
    public static async Task<MaterialisableFlowResult> MaterializeAsync<TResult>(
        this IFlow<TResult> flow,
        MaterializeFlow materialize) where TResult : Task<MaterialisableFlowResult>
    {
        var result = await flow.Execute();
        await materialize.Invoke(result);
        return result;
    }

    public static async Task<TResult> Materialize<TResult>(
        this IFlow<TResult> flow, 
        MaterializeFlow materialize,
        Func<Task>? continueWith) where TResult : MaterialisableFlowResult
    {
        var result = await flow.Materialize(materialize);
        
        if (continueWith is not null)
            await continueWith.Invoke();

        return result;
    }

    public static IMaterialisableFlow PipeTo(this IMaterialisableFlow source, IMaterialisableFlow continuation)
    {
        if (source is FlowIterator iterator)
            return iterator.Next(continuation);

        return new FlowIterator().Next(source).Next(continuation);
    }

    public static IMaterialisableFlow PipeTo(this IMaterialisableFlow source, Func<MaterialisableFlowResult, IMaterialisableFlow> factory)
    {
        var passThroughFlow = new PassThroughFlowDecorator(factory);
        
        if (source is FlowIterator iterator)
            return iterator.Next(passThroughFlow);

        return new FlowIterator().Next(source).Next(passThroughFlow);
    }
    
    public static async Task<TOut> Select<TOut>(this Task<MaterialisableFlowResult> source) where TOut : BaseEntity
    {
        var result = await source;
        return result.Select<TOut>();
    }
    
    public static async Task<IEnumerable<TOut>> SelectMany<TOut>(this Task<MaterialisableFlowResult> source) where TOut : BaseEntity
    {
        var result = await source;
        return result.SelectMany<TOut>()!;
    }
    
    public static TEntity Select<TEntity>(this MaterialisableFlowResult source) where TEntity : BaseEntity
    {
        return source.EntityActions.SingleOrDefault(e => e.Entity is TEntity)?.Entity<TEntity>()!;
    }
    
    public static TEntity Select<TEntity, TAction>(this MaterialisableFlowResult source) where TEntity : BaseEntity
    {
        return source.EntityActions.First(ea => ea.Entity is TEntity && ea is TAction).Entity<TEntity>();
    }
    
    public static IEnumerable<TOut> SelectMany<TOut>(this MaterialisableFlowResult source) where TOut : BaseEntity
    {
        return source.EntityActions.Where(e => e.Entity is TOut).Select(e => e.Entity<TOut>());
    }
    
    public static MaterialisableFlowResult Concat(this MaterialisableFlowResult left, MaterialisableFlowResult right)
    {
        return new MaterialisableFlowResult(
            EntityActions: left.EntityActions.Concat(right.EntityActions),
            IntegrationEvents: left.IntegrationEvents.Concat(right.IntegrationEvents),
            Notifications: left.Notifications.Concat(right.Notifications)
        )
        {
            MediatorEvents = left.MediatorEvents.Concat(right.MediatorEvents),
            AggregateEvents = left.AggregateEvents.Concat(right.AggregateEvents)
        };
    }
    
    public static MaterialisableFlowResult ToFlowResult(this EntityAction source) => new(EntityActions: source.ToEnumerable());

    public static MaterialisableFlowResult ToFlowResult(this IEnumerable<EntityAction> source) => new(EntityActions: source.ToList());

    public static MaterialisableFlowResult ToFlowResult(this BaseIntegrationEvent source) =>
        new(EntityActions: Enumerable.Empty<EntityAction>(), 
            IntegrationEvents: source.ToEnumerable());

    public static MaterialisableFlowResult ToFlowResult(this IEnumerable<BaseIntegrationEvent> source) =>
        new(EntityActions: Enumerable.Empty<EntityAction>(), 
            IntegrationEvents: source);

    public static MaterialisableFlowResult ToFlowResult(this IBaseNotification source) =>
        new(EntityActions: Enumerable.Empty<EntityAction>(), 
            Notifications: source.ToEnumerable());
  
    public static MaterialisableFlowResult ToFlowResult(this IEnumerable<IBaseNotification> source) =>
        new(EntityActions: Enumerable.Empty<EntityAction>(), 
            Notifications: source);

    public static MaterialisableFlowResult ToFlowResult(this INotification source) =>
        new(EntityActions: Enumerable.Empty<EntityAction>()) { MediatorEvents = source.ToEnumerable() };

    public static MaterialisableFlowResult ToFlowResult(this IEnumerable<INotification> source) =>
        new(EntityActions: Enumerable.Empty<EntityAction>()) { MediatorEvents = source };
    
    public static MaterialisableFlowResult ToFlowResult(this IAggregateEvent source) =>
        new(EntityActions: Enumerable.Empty<EntityAction>()) { AggregateEvents = source.ToEnumerable() };
    
    public static MaterialisableFlowResult ToFlowResult(this IEnumerable<IAggregateEvent> source) =>
        new(EntityActions: Enumerable.Empty<EntityAction>()) { AggregateEvents = source };
    
    
    public static EntityAction Added<T>(this IEntityConvertible<T> source) where T : BaseEntity => source.Entity().Added();
    public static EntityAction Updated<T>(this IEntityConvertible<T> source) where T : BaseEntity => source.Entity().Updated();
    public static EntityAction Deleted<T>(this IEntityConvertible<T> source) where T : BaseEntity => source.Entity().Deleted();
}
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Models.Exceptions;

namespace WildHealth.Application.Extensions.Query;

public static class QueryExtensions
{
    /// <summary>
    /// Returns not nullable entity and throws otherwise. 
    /// </summary>
    /// <exception cref="EntityNotFoundException">Throws if not found</exception>
    public static async Task<TSource> FindAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate)
    {
        var entity = await source.SingleOrDefaultAsync(predicate);

        string entityName() => typeof(TSource).Name;
        
        return entity ?? throw new EntityNotFoundException($"{entityName()} not found");
    }
    
    /// <summary>
    /// Returns not nullable entity and throws otherwise. 
    /// </summary>
    /// <exception cref="EntityNotFoundException">Throws if not found</exception>
    public static async Task<TSource> FindAsync<TSource>(this IQueryable<TSource> source)
    {
        return await source.FindAsync(_ => true);
    }
    
    /// <summary>
    /// Query the source using the specific Query Flow
    /// </summary>
    public static IQueryable<TResult> Query<TSource, TResult>(
        this IQueryable<TSource> source, 
        Func<IQueryable<TSource>, IQueryFlow<TResult>> factory)
    {
        var queryFlow = factory(source);
        return queryFlow.Execute();
    }
}
using System;
using System.Linq;
using System.Collections.Generic;

namespace WildHealth.Application.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<IEnumerable<T>> Split<T>(this T[] arr, int size)
    {
        for (var i = 0; i < arr.Length / size + 1; i++) {
            yield return arr.Skip(i * size).Take(size);
        }
    }

    public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T newItem)
    {
        return source.Concat(new[] { newItem });
    }
    
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
    {
        return source.Select((item, index) => (item, index));
    }
    
    public static bool Empty<T>(this IEnumerable<T>? source)
    {
        return source is null || !source.Any();
    }
    
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) => 
        source == null || !source.GetEnumerator().MoveNext();
    
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> f)
    {
        foreach (var x in source) f(x);
    }
    
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> property, Func<T, TKey> orderBy)
    {
        return source.GroupBy(property).Select(x => x.OrderBy(orderBy).First());
    }
    
    public static bool NotExists<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return !source.Any(predicate);
    }
    
    public static IEnumerable<(TKey, List<TItem>)> PartitionBy<TItem, TKey, TOrderBy>(this IEnumerable<TItem> items, Func<TItem, TKey> keySelector, Func<TItem, TOrderBy> orderBy)
    {
        var currentPartition = new List<TItem>();
        foreach (var item in items.OrderBy(orderBy))
        {
            var currentKey = keySelector(item);
            if (currentPartition.Empty() || Equals(keySelector(currentPartition[0]), currentKey))
            {
                currentPartition.Add(item);
            }
            else if (currentPartition.Any())
            {
                yield return (keySelector(currentPartition[0]), currentPartition);
                currentPartition = new List<TItem>{ item };
            }
        }

        if (currentPartition.Any())
            yield return (keySelector(currentPartition[0]), currentPartition);
    }
}

public static class List
{
    public static List<T> Empty<T>() => new();
}
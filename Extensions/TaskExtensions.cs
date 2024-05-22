using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WildHealth.Application.Extensions;

public static class TaskExtensions
{
    public static async Task<TResult> SelectMany<TSource1, TSource2, TResult>(
        this Task<TSource1> source1, 
        Func<TSource1, Task<TSource2>> source2,
        Func<TSource1, TSource2, TResult> resultSelector)
    {
        var result1 = await source1;
        var result2 = await source2(result1);
        return resultSelector(result1, result2);
    }
    
    public static async ValueTask<(T, TimeSpan)> Measure<T>(this ValueTask<T> source)
    {
        var sw = Stopwatch.StartNew();
        var result = await source;
        sw.Stop();
        return (result, sw.Elapsed);
    }

    public static async ValueTask<(T, TimeSpan)> Measure<T>(this Task<T> source)
    {
        var sw = Stopwatch.StartNew();
        var result = await source;
        sw.Stop();
        return (result, sw.Elapsed);
    }
}
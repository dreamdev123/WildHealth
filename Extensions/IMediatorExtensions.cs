using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace WildHealth.Application.Extensions;

public static class IMediatorExtensions
{
    public static async Task PublishAll<TNotification>(
        this IMediator source, 
        IEnumerable<TNotification> notifications,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var tasks = notifications.Select(n => source.Publish(n, cancellationToken));
        await Task.WhenAll(tasks);
    }
}
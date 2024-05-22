using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WildHealth.Shared.DistributedCache;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Utils.Locker;

/// <summary>
/// <see cref="ILocker"/>
/// </summary>
public class Locker : ILocker, IDisposable
{
    private const string Locked = "LOCKED";
    
    private readonly TimeSpan _expiration = new(TimeSpan.TicksPerHour);
    
    private readonly IConnectionMultiplexer _connection;
    
    public Locker(IOptions<AzureRedisCacheOptions> options)
    {
        _connection = ConnectionMultiplexer.Connect(options.Value.ConnectionString);
    }

    /// <summary>
    /// <see cref="ILocker.LockAsync"/>
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<bool> LockAsync(string key)
    {
        var existing = await _connection.GetDatabase().StringGetAsync(key);

        if (existing == Locked)
        {
            throw new AppException(HttpStatusCode.Locked, "Object is locked by another process");
        }
        
        await _connection.GetDatabase().StringSetAsync(key, Locked, _expiration, When.NotExists);
        
        return true;
    }

    /// <summary>
    /// <see cref="ILocker.UnlockAsync"/>
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task UnlockAsync(string key)
    {
        await _connection.GetDatabase().KeyDeleteAsync(key);
    }

    #region dispose
        
    private bool _disposed;

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _connection.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
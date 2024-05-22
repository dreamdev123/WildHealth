using System.Threading.Tasks;

namespace WildHealth.Application.Utils.Locker;

/// <summary>
/// Provides util to lock processes or entities
/// </summary>
public interface ILocker
{
    /// <summary>
    /// Locks process or entity by key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task<bool> LockAsync(string key);
    
    /// <summary>
    /// Unlocks process or entity by key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task UnlockAsync(string key);
}
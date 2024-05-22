namespace WildHealth.Application.Utils.ServiceHelpers;

public interface IServiceHelper<T>
{
    void ThrowIfNotExist(T? val, string keyName, object? keyValue);
}
using System.Net;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Utils.ServiceHelpers;

public class ServiceHelper<T> : IServiceHelper<T>
{
    public void ThrowIfNotExist(T? val, string keyName, object? keyValue)
    {
        if (val is null)
        {
            var exceptionParam = new AppException.ExceptionParameter(keyName, keyValue);
            
            throw new AppException(HttpStatusCode.NotFound, $"{typeof(T)} with key does not exist", exceptionParam);
        }
    }
}
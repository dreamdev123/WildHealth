using System.Threading.Tasks;
using WildHealth.Domain.Entities.EmployerProducts;

namespace WildHealth.Application.Utils.DefaultEmployerProvider;

/// <summary>
/// Represents default employer product provider
/// </summary>
public interface IDefaultEmployerProvider
{
    /// <summary>
    /// Returns default employer product
    /// </summary>
    /// <returns></returns>
    Task<EmployerProduct?> GetAsync();
}
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Products;
using WildHealth.Domain.Enums.Products;

namespace WildHealth.Application.Services.Products;

/// <summary>
/// Provides methods for working with products
/// </summary>
public interface IProductsService
{
    /// <summary>
    /// Returns product by practice and own identifier
    /// </summary>
    /// <param name="practiceId"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Product> GetAsync(int practiceId, int id);
    
    /// <summary>
    /// Returns product by practice and product type
    /// </summary>
    /// <param name="type"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<Product> GetAsync(ProductType type, int practiceId);
    
    /// <summary>
    /// Returns products by practiceId
    /// </summary>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<Product[]> GetAsync(int practiceId);
}
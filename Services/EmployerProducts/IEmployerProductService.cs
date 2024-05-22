using System.Threading.Tasks;
using WildHealth.Domain.Entities.EmployerProducts;

namespace WildHealth.Application.Services.EmployerProducts;

/// <summary>
/// Provides methods for working with employer products
/// </summary>
public interface IEmployerProductService
{
    /// <summary>
    /// Returns All Active Employer Products
    /// </summary>
    /// <returns></returns>
    public Task<EmployerProduct[]> GetEmployerProductsAsync();
    
    /// <summary>
    /// Returns Active Employer Products by Ids
    /// </summary>
    /// <returns></returns>
    public Task<EmployerProduct[]> GetEmployerProductsByIdsAsync(int[] ids);

    /// <summary>
    /// Returns Employer Product by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<EmployerProduct> GetByIdAsync(int id);

    /// <summary>
    /// Returns Employer Product by key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task<EmployerProduct> GetByKeyAsync(string? key = null);

    /// <summary>
    /// Creates employer product
    /// </summary>
    /// <param name="employerProduct"></param>
    /// <returns></returns>
    public Task<EmployerProduct> CreateAsync(EmployerProduct employerProduct);
    
    /// <summary>
    /// Updates employer product
    /// </summary>
    /// <param name="employerProduct"></param>
    /// <returns></returns>
    public Task<EmployerProduct> UpdateAsync(EmployerProduct employerProduct);
}
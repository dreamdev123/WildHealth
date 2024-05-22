using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Products;
using WildHealth.Domain.Enums.Products;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Products;

/// <summary>
/// <see cref="IProductsService"/>
/// </summary>
public class ProductsService : IProductsService
{
    private readonly IGeneralRepository<Product> _productRepository;

    public ProductsService(IGeneralRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// <see cref="IProductsService.GetAsync(int, int)"/>
    /// </summary>
    /// <param name="practiceId"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Product> GetAsync(int practiceId, int id)
    {
        var result = await _productRepository
            .All()
            .ById(id)
            .RelatedToPractice(practiceId)
            .FirstOrDefaultAsync();
        
        if (result is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "Product does not exist");
        }

        return result;
    }

    /// <summary>
    /// <see cref="IProductsService.GetAsync(ProductType, int)"/>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    public async Task<Product> GetAsync(ProductType type, int practiceId)
    {
        var result = await _productRepository
            .All()
            .RelatedToPractice(practiceId)
            .FirstOrDefaultAsync(x => x.Type == type);
        
        if (result is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "Product does not exist");
        }

        return result;
    }

    /// <summary>
    /// <see cref="IProductsService.GetAsync(int)"/>
    /// </summary>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    public async Task<Product[]> GetAsync(int practiceId)
    {
        var result = await _productRepository
            .All()
            .RelatedToPractice(practiceId)
            .ToArrayAsync();
        
        return result;
    }
}
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Utils.ServiceHelpers;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.EmployerProducts;

public class EmployerProductService : IEmployerProductService
{
    private readonly IGeneralRepository<EmployerProduct> _repository;
    private readonly IServiceHelper<EmployerProduct> _serviceHelper;

    public EmployerProductService(
        IGeneralRepository<EmployerProduct> repository,
        IServiceHelper<EmployerProduct> serviceHelper)
    {
        _repository = repository;
        _serviceHelper = serviceHelper;
    }

    /// <summary>
    /// <see cref="IEmployerProductService.GetEmployerProductsAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<EmployerProduct[]> GetEmployerProductsAsync()
    {
        var employerProducts = await _repository
            .All()
            .ApplyIncludes()
            .ToArrayAsync();

        return employerProducts;
    }

    public async Task<EmployerProduct[]> GetEmployerProductsByIdsAsync(int[] ids)
    {
        var employerProducts = await _repository
            .All()
            .ByIds(ids)
            .ApplyIncludes()
            .ToArrayAsync();

        return employerProducts;
    }

    /// <summary>
    /// <see cref="IEmployerProductService.GetByIdAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<EmployerProduct> GetByIdAsync(int id)
    {
        var result = await _repository
            .All()
            .ById(id)
            .ApplyIncludes()
            .FirstOrDefaultAsync();

        _serviceHelper.ThrowIfNotExist(result, nameof(id), id);

        return result!;
    }

    /// <summary>
    /// <see cref="IEmployerProductService.GetByKeyAsync"/>
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<EmployerProduct> GetByKeyAsync(string? key = null)
    {
        var result = await _repository
            .All()
            .ApplyIncludes()
            .ByKeyOrDefault(key)
            .FirstOrDefaultAsync();
        
        _serviceHelper.ThrowIfNotExist(result, nameof(key), key);

        return result!;
    }

    /// <summary>
    /// <see cref="IEmployerProductService.CreateAsync"/>
    /// </summary>
    /// <param name="employerProduct"></param>
    /// <returns></returns>
    public async Task<EmployerProduct> CreateAsync(EmployerProduct employerProduct)
    {
        await _repository.AddAsync(employerProduct);

        await _repository.SaveAsync();

        return employerProduct;
    }

    /// <summary>
    /// <see cref="IEmployerProductService.UpdateAsync"/>
    /// </summary>
    /// <param name="employerProduct"></param>
    /// <returns></returns>
    public async Task<EmployerProduct> UpdateAsync(EmployerProduct employerProduct)
    {
        _repository.Edit(employerProduct);

        await _repository.SaveAsync();
        
        return employerProduct;
    }
}
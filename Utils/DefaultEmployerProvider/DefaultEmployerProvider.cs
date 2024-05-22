using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Utils.DefaultEmployerProvider;

/// <summary>
/// <see cref="IDefaultEmployerProvider"/>
/// </summary>
public class DefaultEmployerProvider : IDefaultEmployerProvider
{
    private readonly IGeneralRepository<EmployerProduct> _repository;

    public DefaultEmployerProvider(IGeneralRepository<EmployerProduct> repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// <see cref="IDefaultEmployerProvider.GetAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<EmployerProduct?> GetAsync()
    {
        return await _repository
            .All()
            .ApplyIncludes()
            .ByKeyOrDefault(null)
            .FirstOrDefaultAsync();
    }
}
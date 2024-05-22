using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Enums.Insurance;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Coverages;

/// <summary>
/// <see cref="ICoveragesService"/>
/// </summary>
public class CoveragesService : ICoveragesService
{
    private readonly IGeneralRepository<Coverage> _coveragesRepository;

    public CoveragesService(IGeneralRepository<Coverage> coveragesRepository)
    {
        _coveragesRepository = coveragesRepository;
    }

    /// <summary>
    /// <see cref="ICoveragesService.GetAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Coverage> GetAsync(int id)
    {
        var coverage = await _coveragesRepository
            .All()
            .ById(id)
            .IncludeInsurance()
            .IncludeVerifications()
            .IncludeUser()
            .IncludePatient()
            .FirstOrDefaultAsync();

        if (coverage is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "Coverage does not exist");
        }

        return coverage;
    }

    /// <summary>
    /// <see cref="ICoveragesService.GetPrimaryAsync"/>
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<Coverage[]> GetPrimaryAsync(int userId)
    {
        var coverage = await _coveragesRepository
            .All()
            .RelatedToUser(userId)
            .ByPriority(CoveragePriority.Primary)
            .IncludeInsurance()
            .IncludeVerifications()
            .IncludeUser()
            .IncludePatient()
            .ToArrayAsync();

        return coverage;
    }

    /// <summary>
    /// <see cref="ICoveragesService.GetAllAsync"/>
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<Coverage[]> GetAllAsync(int userId)
    {
        return await _coveragesRepository
            .All()
            .IncludeInsurance()
            .IncludeVerifications()
            .RelatedToUser(userId)
            .ToArrayAsync();
    }

    /// <summary>
    /// <see cref="ICoveragesService.CreateAsync"/>
    /// </summary>
    /// <param name="coverage"></param>
    /// <returns></returns>
    public async Task<Coverage> CreateAsync(Coverage coverage)
    {
        await _coveragesRepository.AddAsync(coverage);

        await _coveragesRepository.SaveAsync();

        return coverage;
    }
    
    /// <summary>
    /// <see cref="ICoveragesService.UpdateAsync"/>
    /// </summary>
    /// <param name="coverage"></param>
    /// <returns></returns>
    public async Task<Coverage> UpdateAsync(Coverage coverage)
    {
        _coveragesRepository.Edit(coverage);

        await _coveragesRepository.SaveAsync();

        return coverage;
    }
}
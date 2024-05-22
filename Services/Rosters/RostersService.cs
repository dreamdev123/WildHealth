using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Rosters;

/// <summary>
/// <see cref="IRostersService"/>
/// </summary>
public class RostersService : IRostersService
{
    private readonly IGeneralRepository<Roster> _rostersRepository;

    public RostersService(IGeneralRepository<Roster> rostersRepository)
    {
        _rostersRepository = rostersRepository;
    }

    /// <summary>
    /// <see cref="IRostersService.SelectAsync"/>
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    public async Task<Roster[]> SelectAsync(bool? isActive = null)
    {
        var rosters = await _rostersRepository
            .All()
            .ByActive(isActive)
            .OrderByDescending(x => x.Id)
            .AsNoTracking()
            .ToArrayAsync();

        return rosters;
    }

    /// <summary>
    /// <see cref="IRostersService.GetAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Roster> GetAsync(int id)
    {
        var roster = await _rostersRepository
            .All()
            .ById(id)
            .FirstOrDefaultAsync();

        if (roster is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "Roster does not exist");
        }

        return roster;
    }

    /// <summary>
    /// <see cref="IRostersService.CreateAsync"/>
    /// </summary>
    /// <param name="roster"></param>
    /// <returns></returns>
    public async Task<Roster> CreateAsync(Roster roster)
    {
        await _rostersRepository.AddAsync(roster);
        
        await _rostersRepository.SaveAsync();

        return roster;
    }

    /// <summary>
    /// <see cref="IRostersService.UpdateAsync"/>
    /// </summary>
    /// <param name="roster"></param>
    /// <returns></returns>
    public async Task<Roster> UpdateAsync(Roster roster)
    {
        _rostersRepository.Edit(roster);
        
        await _rostersRepository.SaveAsync();

        return roster;
    }
}
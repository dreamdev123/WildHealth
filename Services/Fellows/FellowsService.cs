using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Fellows;

public class FellowsService : IFellowsService
{
    private readonly IGeneralRepository<Fellow> _repository;

    public FellowsService(IGeneralRepository<Fellow> repository)
    {
        _repository = repository;
    }

    public Task<Fellow> GetByIdAsync(int id)
    {
        return _repository.All().ById(id).FindAsync();
    }

    public Task<Fellow[]> GetByRosterIdAsync(int rosterId)
    {
        return _repository
            .All()
            .NotDeleted()
            .GetByRosterId(rosterId)
            .OrderByUserName()
            .AsNoTracking()
            .ToArrayAsync();
    }

    public Task<Fellow[]> GetAsync(DateTime? start = null, DateTime? end = null, bool? activeRoster = null)
    {
        return _repository
            .All()
            .NotDeleted()
            .ByCreationDate(start, end)
            .ByActiveRoster(activeRoster)
            .OrderByUserName()
            .AsNoTracking()
            .ToArrayAsync();
    }

    public async Task<Fellow> DeleteAsync(Fellow fellow)
    {
        _repository.Delete(fellow);

        await _repository.SaveAsync();

        return fellow;
    }
}
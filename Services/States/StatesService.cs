using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Address;
using WildHealth.Domain.Exceptions;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.States;

public class StatesService : IStatesService
{
    private readonly IGeneralRepository<State> _statesRepository;

    public StatesService(IGeneralRepository<State> statesRepository)
    {
        _statesRepository = statesRepository;
    }

    public async Task<State> GetByName(string name)
    {
        var state = await _statesRepository
            .All()
            .Where(o => o.Name.ToUpper() == name.ToUpper())
            .FirstOrDefaultAsync();

        if (state is null)
        {
            throw new DomainException($"State with name={name} does not exist.");
        }

        return state;
    }

    public async Task<State> GetByAbbreviation(string abbreviation)
    {
        var state = await _statesRepository
            .All()
            .Where(o => o.Abbreviation.ToUpper() == abbreviation.ToUpper())
            .FirstOrDefaultAsync();

        if (state is null)
        {
            throw new DomainException($"State with abbreviation={abbreviation} does not exist.");
        }

        return state;
    }

    public async Task<State> GetByValue(string value)
    {
        try
        {
            return await GetByName(value);
        }
        catch (DomainException)
        {
            return await GetByAbbreviation(value);
        }
    }
}
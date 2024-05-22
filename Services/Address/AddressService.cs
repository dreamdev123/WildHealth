using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Address;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Address;

public class AddressService : IAddressService
{
    private readonly IGeneralRepository<EmployeeState> _employeeStatesRepository;
    private readonly IGeneralRepository<State> _statesRepository;

    public AddressService(
        IGeneralRepository<EmployeeState> employeeStatesRepository,
        IGeneralRepository<State> statesRepository)
    {
        _employeeStatesRepository = employeeStatesRepository;
        _statesRepository = statesRepository;
    }

    /// <summary>
    /// <see cref="IAddressService.GetStatesAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<State[]> GetStatesAsync(string? searchQuery)
    {
        var query = _statesRepository.All();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(x => x.Name.ToLower().Contains(searchQuery.ToLower()) 
                                     || x.Abbreviation.ToLower().Contains(searchQuery.ToLower()));
        }
            
        var result = await query.ToArrayAsync();

        return result;
    }

    /// <summary>
    /// <see cref="IAddressService.UpdateEmployeeStatesAsync"/>
    /// </summary>
    /// <param name="statesIds"></param>
    /// <param name="employee"></param>
    /// <returns></returns>
    public async Task<EmployeeState[]> UpdateEmployeeStatesAsync(int[] statesIds, Employee employee)
    {
        var currentRecords = await _employeeStatesRepository
            .All()
            .Where(x => x.EmployeeId == employee.Id)
            .ToArrayAsync();

        foreach (var record in currentRecords)
        {
            if (statesIds.Any(statesId=> statesId == record.StateId))
            {
                continue;
            }
            
            _employeeStatesRepository.Delete(record);
        }

        foreach (var stateId in statesIds)
        {
            if (currentRecords.Any(x=> x.StateId == stateId))
            {
                continue;
            }
            
            await _employeeStatesRepository.AddAsync(new EmployeeState(employee.GetId(), stateId));
        }

        await _employeeStatesRepository.SaveAsync();

        return await _employeeStatesRepository
            .All()
            .Where(x => x.EmployeeId == employee.GetId())
            .ToArrayAsync();
    }
}
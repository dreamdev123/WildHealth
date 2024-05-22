using System.Threading.Tasks;
using WildHealth.Domain.Entities.Address;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Services.Address;

public interface IAddressService
{
    /// <summary>
    /// Returns an array of US states.
    /// </summary>
    /// <returns></returns>
    Task<State[]> GetStatesAsync(string? searchQuery);

    /// <summary>
    /// Updates employee states
    /// </summary>
    /// <param name="statesIds"></param>
    /// <param name="employee"></param>
    /// <returns></returns>
    Task<EmployeeState[]> UpdateEmployeeStatesAsync(int[] statesIds, Employee employee);
}
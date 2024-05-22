using System.Threading.Tasks;
using WildHealth.Domain.Entities.Address;

namespace WildHealth.Application.Services.States;

public interface IStatesService
{
    /// <summary>
    /// Returns state 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<State> GetByName(string name);

    /// <summary>
    /// Returns state by abbreviation
    /// </summary>
    /// <param name="abbreviation"></param>
    /// <returns></returns>
    Task<State> GetByAbbreviation(string abbreviation);

    /// <summary>
    /// Returns state by either abbreviation or whole name
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<State> GetByValue(string value);
}
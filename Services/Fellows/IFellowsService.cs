using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Services.Fellows;

public interface IFellowsService
{
    Task<Fellow> GetByIdAsync(int id);
    
    Task<Fellow[]> GetByRosterIdAsync(int rosterId);
    
    Task<Fellow[]> GetAsync(
        DateTime? start = null, 
        DateTime? end = null, 
        bool? activeRoster = null);

    Task<Fellow> DeleteAsync(Fellow fellow);
}
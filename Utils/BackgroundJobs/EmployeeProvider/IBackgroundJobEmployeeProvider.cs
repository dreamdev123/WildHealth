using System.Threading.Tasks;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Utils.BackgroundJobs.EmployeeProvider;

public interface IBackgroundJobEmployeeProvider
{

    /// <summary>
    /// Returns Background job employee and create if not exist
    /// </summary>
    /// <returns></returns>
    Task<Employee> GetBackgroundJobEmployee();

}
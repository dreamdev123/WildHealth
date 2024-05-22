using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Employees;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Shared.Data.Helpers;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Services.Employees
{
    /// <summary>
    /// Provides method for working with employees
    /// </summary>
    public interface IEmployeeService
    {
        /// <summary>
        /// Returns employees by query
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="practiceId"></param>
        /// <param name="locationIds"></param>
        /// <param name="roleIds"></param>
        /// <returns></returns>
        Task<IEnumerable<Employee>> GetEmployeesAsync(string? searchQuery, int practiceId, int[]? locationIds, int[] roleIds);

        /// <summary>
        /// Returns all employees from target practice 
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<IEnumerable<Employee>> GetAllPracticeEmployeesAsync(int practiceId);
        
        /// <summary>
        /// Returns all active employees in the system
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Employee>> GetAllActiveEmployeesAsync();

        /// <summary>
        /// Returns employees by permissions
        /// </summary>
        /// <param name="permissions"></param>
        /// <param name="practiceIdId"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        Task<IEnumerable<Employee>> GetEmployeesByPermissionsAsync(PermissionType[] permissions, int practiceIdId, int locationId);

        /// <summary>
        /// Returns active coaches and providers
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="locationIds"></param>
        /// <returns></returns>
        Task<IEnumerable<Employee>> GetCoachesAndProvidersAsync(int practiceId, int[] locationIds);
        
        /// <summary>
        /// Returns active employees by role
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="locationIds"></param>
        /// <param name="roleIds"></param>
        /// <returns></returns>
        Task<IEnumerable<Employee>> GetByRoleIdsAsync(int practiceId, int[] locationIds, int[] roleIds);

        /// <summary>
        /// Returns active employees by a collection of roles
        /// </summary>
        /// <param name="roleIds"></param>
        /// <returns></returns>
        Task<IEnumerable<Employee>> GetByRolesIdsAsync(params int[] roleIds);
        
        /// <summary>
        /// Return employees by ids
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="practiceId"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        Task<IEnumerable<Employee>> GetActiveAsync(IEnumerable<int> ids, int practiceId, int locationId);

        /// <summary>
        /// Returns employees assigned to patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<Employee[]> GetAssignedToAsync(int patientId);

        /// <summary>
        /// Returns employee by id includes user information
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Employee> GetByIdAsync(int id);

        /// <summary>
        /// Returns employee by id with given specification
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        Task<Employee> GetByIdAsync(int id, ISpecification<Employee> specification);

        /// <summary>
        /// Returns employee(include inactive employee) by id includes user information
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Employee> GetEmployeeInfoByIdAsync(int id);
        
        Task<Employee[]> GetEmployeesInfoByIdAsync(int[] ids);

        /// <summary>
        /// Returns employees by ids includes user information
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        Task<Employee[]> GetByIdsAsync(int[] ids, ISpecification<Employee> specification);

        /// <summary>
        /// Returns employee by user id includes user information
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Employee> GetByUserIdAsync(int id);

        /// <summary>
        /// Returns employee by integration id includes user information
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <param name="practiceId"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        Task<Employee?> GetByIntegrationIdAsync(string integrationId,
            IntegrationVendor vendor,
            string purpose,
            int practiceId,
            int locationId);

        /// <summary>
        /// Returns employee by integration id includes user information
        /// </summary>
        /// <param name="schedulerAccountId"></param>
        /// <returns></returns>
        Task<Employee?> GetBySchedulerAccountIdAsync(string schedulerAccountId);

        /// <summary>
        /// Returns employee by user id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        Task<Employee> GetByUserIdAsync(int id, ISpecification<Employee> specification);

        /// <summary>
        /// Creates and returns employee
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        Task<Employee> CreateAsync(Employee employee);
        
        /// <summary>
        /// Updates employee
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        Task<Employee> UpdateAsync(Employee employee);

        /// <summary>
        /// Deletes employee
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        Task DeleteAsync(Employee employee);

        /// <summary>
        /// Returns employee dashboard data
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        public Task<EmployeeDashboardModel> GetEmployeeDashboardModel(int employeeId);
    }
}
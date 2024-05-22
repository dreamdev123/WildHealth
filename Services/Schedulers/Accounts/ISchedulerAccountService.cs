using System.Threading.Tasks;
using WildHealth.Common.Models.Scheduler;
using WildHealth.Domain.Entities.Employees;
using WildHealth.TimeKit.Clients.Models.Resources;

namespace WildHealth.Application.Services.Schedulers.Accounts
{
    /// <summary>
    /// Provides methods for scheduler accounts managing
    /// </summary>
    public interface ISchedulerAccountService
    {
        /// <summary>
        /// Registers scheduler account for user and returns account id with password
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<SchedulerRegistrationResultModel> RegisterAccountAsync(RegisterSchedulerAccountModel model);

        /// <summary>
        /// <see cref="ISchedulerAccountService.DeleteAccountAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<bool> DeleteAccountAsync(DeleteSchedulerAccountModel model);
        
        /// <summary>
        /// Returns scheduler account related to employee
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        Task<ResourceModel> GetAccountAsync(Employee employee);

        /// <summary>
        /// Returns resources from scheduler system filtered by email
        /// </summary>
        /// <returns></returns>
        Task<ResourceModel[]> GetResourcesByEmailAsync(Employee employee);
        
        /// <summary>
        /// Returns list of accounts from the Timekit
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<ResourceAccountModel[]> GetResourcesAsync(int practiceId);
    }
}

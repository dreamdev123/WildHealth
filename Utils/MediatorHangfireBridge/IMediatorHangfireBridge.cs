using System.Threading.Tasks;
using WildHealth.Domain.Entities.Engagement;

namespace WildHealth.Application.Utils.MediatorHangfireBridge
{
    /// <summary>
    /// Provides bridge between Mediator and Hangfire
    /// </summary>
    public interface IMediatorHangfireBridge
    {
        /// <summary>
        /// Executes Renew Subscriptions Command
        /// </summary>
        /// <returns></returns>
        Task ExecuteRenewSubscriptionsCommand(string jobId);
        
        /// <summary>
        /// Execute subscriptions cancellation requests command
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        Task ExecuteCancellationRequestProcessCommand(string jobId);

        /// <summary>
        /// Execute PatientEngagements generation command
        /// </summary>
        /// <returns></returns>
        Task ExecutePatientEngagementScannersCommand(string jobId, EngagementAssignee assignee);
    }
}
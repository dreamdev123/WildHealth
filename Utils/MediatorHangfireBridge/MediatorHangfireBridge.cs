using System;
using System.ComponentModel;
using System.Threading.Tasks;
using WildHealth.Application.Commands.PatientEngagements;
using WildHealth.Application.Commands.Questionnaires;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.BackgroundJobs.CommandsScheduler;
using WildHealth.Domain.Entities.Engagement;

namespace WildHealth.Application.Utils.MediatorHangfireBridge
{
    /// <summary>
    /// <see cref = "IMediatorHangfireBridge" />
    /// </summary>
    public class MediatorHangfireBridge : IMediatorHangfireBridge
    {
        private readonly ICommandsScheduler _commandsScheduler;

        public MediatorHangfireBridge(ICommandsScheduler commandsScheduler)
        {
            _commandsScheduler = commandsScheduler;
        }

        /// <summary>
        /// <see cref = "IMediatorHangfireBridge.ExecuteRenewSubscriptionsCommand" />
        /// </summary>
        /// <returns></returns>
        [DisplayName("JobID: {0}")]
        public Task ExecuteRenewSubscriptionsCommand(string jobId = nameof(SendQuestionnaireReminderCommand))
        {
            var command = new RenewSubscriptionsCommand(DateTime.UtcNow);

            return _commandsScheduler.ScheduleCommandAsync(jobId, command);
        }

        /// <summary>
        /// <see cref="IMediatorHangfireBridge.ExecuteCancellationRequestProcessCommand"/>
        /// </summary>
        /// <returns></returns>
        [DisplayName("JobID: {0}")]
        public Task ExecuteCancellationRequestProcessCommand(string jobId = nameof(ExecuteSubscriptionCancellationRequestsCommand))
        {
            var command = new ExecuteSubscriptionCancellationRequestsCommand(DateTime.UtcNow);

            return _commandsScheduler.ScheduleCommandAsync(jobId, command);
        }
        
        /// <summary>
        /// <see cref="IMediatorHangfireBridge.ExecutePatientEngagementScannersCommand"/>
        /// </summary>
        /// <returns></returns>
        [DisplayName("JobID: {0}")]
        public Task ExecutePatientEngagementScannersCommand(string jobId, EngagementAssignee assignee)
        {
            var command = new RunPatientEngagementScannersCommand(DateTime.UtcNow, assignee);

            return _commandsScheduler.ScheduleCommandAsync(jobId, command);
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Events.Payments;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using MediatR;

namespace WildHealth.Application.EventHandlers.Payments
{
    public class SendReportOnRenewalSubscriptionEventHandler : INotificationHandler<RenewalSubscriptionFinishEvent>
    {
        private readonly IMediator _mediator;
        private readonly ISettingsManager _settingsManager;

        public SendReportOnRenewalSubscriptionEventHandler(
            IMediator mediator,
            ISettingsManager settingsManager)
        {
            _mediator = mediator;
            _settingsManager = settingsManager;
        }
        
        public async Task Handle(RenewalSubscriptionFinishEvent notification, CancellationToken cancellationToken)
        {
            var isSendRenewalSubscriptionReportEnabled = await _settingsManager.GetSetting<bool>(
                key: SettingsNames.General.SendRenewalSubscriptionReport, 
                practiceId: notification.PracticeId);
            
            if(!isSendRenewalSubscriptionReportEnabled)
            {
                return;
            }
            
            var command = new SendRenewalSubscriptionReportCommand(
                renewedSubscription: notification.RenewedSubscription,
                practiceId: notification.PracticeId);

            await _mediator.Send(command, cancellationToken);
        }
    }
}
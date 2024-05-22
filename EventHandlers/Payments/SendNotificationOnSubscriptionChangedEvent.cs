using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Payments;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using MediatR;

namespace WildHealth.Application.EventHandlers.Payments
{
    public class SendNotificationOnSubscriptionChangedEvent : INotificationHandler<SubscriptionChangedEvent>
    {
        private readonly INotificationService _notificationService;

        public SendNotificationOnSubscriptionChangedEvent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(SubscriptionChangedEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.CreateNotificationAsync(new ChangeHealthPlanNotification(notification.Patient));
        }
    }
}

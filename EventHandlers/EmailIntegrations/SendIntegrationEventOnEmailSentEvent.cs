using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.EmailIntegrations;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Emails;
using WildHealth.IntegrationEvents.Emails.Payloads;
using MediatR;

namespace WildHealth.Application.EventHandlers.EmailIntegrations
{
    public class SendIntegrationEventOnEmailSentEvent : INotificationHandler<EmailSentEvent>
    {
        private readonly IEventBus _eventBus;

        public SendIntegrationEventOnEmailSentEvent()
        {
            _eventBus = EventBusProvider.Get();
        }

        public async Task Handle(EmailSentEvent notification, CancellationToken cancellationToken)
        {
            var user = notification.User;

            await _eventBus.Publish( new EmailIntegrationEvent(
                payload: new TransactionalEmailSentPayload(
                    subject: notification.Subject,
                    emailTemplateType: notification.EmailTemplateTypeName
                ),
                user: new UserMetadataModel(user.UniversalId.ToString()),
                eventDate: DateTime.UtcNow
            ), cancellationToken);
        }
    }
}
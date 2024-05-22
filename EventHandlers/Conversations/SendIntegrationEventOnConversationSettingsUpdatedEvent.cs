using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.IntegrationEvents.ConversationSettings;
using WildHealth.IntegrationEvents.ConversationSettings.Payloads;
using MediatR;

namespace WildHealth.Application.EventHandlers.Conversations
{
    public class SendIntegrationEventOnConversationSettingsUpdatedEvent : INotificationHandler<ConversationSettingsUpdatedEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly IConversationsService _conversationService;
        private readonly IEmployeeService _employeeService;
        private readonly IConversationsSettingsService _conversationsSettingsService;

        public SendIntegrationEventOnConversationSettingsUpdatedEvent(
            IConversationsService conversationsService,
            IEmployeeService employeeService,
            IConversationsSettingsService conversationsSettingsService)
        {
            _eventBus = EventBusProvider.Get();
            _conversationService = conversationsService;
            _employeeService = employeeService;
            _conversationsSettingsService = conversationsSettingsService;
        }

        public async Task Handle(ConversationSettingsUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var conversationSettings = await _conversationsSettingsService.GetByEmployeeIdLegacy(notification.EmployeeId);
            var specification = ConversationSpecifications.ParticipantsAndDevices;
            var conversations = await _conversationService.GetConversationsByEmployeeAsync(
                    employeeId: conversationSettings.EmployeeId,
                    specification: specification, 
                    isActive: true);
            var employee = await _employeeService.GetByIdAsync(conversationSettings.EmployeeId);
            var vendorExternalIdentity = employee.User.MessagingIdentity();
            var deviceTokens = conversations.SelectMany(c => c.PatientParticipants)
                                .SelectMany(x => x.Patient.User.Devices)
                                .Select(d => d.DeviceToken)
                                .Distinct()
                                .ToArray();

            await _eventBus.Publish(new ConversationSettingsIntegrationEvent(
                payload: new ConversationSettingsUpdatedPayload(
                    employeeId: conversationSettings.EmployeeId,
                    messageEnabled: conversationSettings.MessageEnabled,
                    message: conversationSettings.Message,
                    forwardEmployeeEnabled: conversationSettings.ForwardEmployeeEnabled,
                    forwardEmployeeId: conversationSettings.ForwardEmployeeId,
                    vendorExternalIdentity: vendorExternalIdentity,
                    deviceTokens: deviceTokens
                ),
                eventDate: DateTime.UtcNow), cancellationToken);
        }
    }
}
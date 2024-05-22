using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Devices;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Conversations;
using WildHealth.IntegrationEvents.Conversations.Payloads;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using MediatR;

namespace WildHealth.Application.EventHandlers.Conversations
{
    public class SendIntegrationEventOnConversationUpdatedEvent : INotificationHandler<ConversationUpdatedEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly IDevicesService _devicesService;

        public SendIntegrationEventOnConversationUpdatedEvent(IDevicesService devicesService)
        {
            _devicesService = devicesService;
            _eventBus = EventBusProvider.Get();
        }

        public async Task Handle(ConversationUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var conversation = notification.Conversation;
            var patientUsers = conversation.PatientParticipants.Select(x => x.Patient.UserId);
            var devices = await _devicesService.GetConversationDevices(conversation.VendorExternalId, null);
            var deviceTokens = devices
                .Where(x => patientUsers.Contains(x.UserId))
                .Select(d => d.DeviceToken)
                .ToArray();

            await _eventBus.Publish(new ConversationIntegrationEvent(
                payload: new ConversationUpdatedPayload(
                        subject: conversation.Subject,
                        locationId: conversation.LocationId,
                        startDate: conversation.StartDate,
                        lastMessageAt: conversation.LastMessageAt,
                        vendorExternalId: conversation.VendorExternalId,
                        type: conversation.Type.ToString(),
                        state: conversation.State.ToString(),
                        stateChangeDate: conversation.StateChangeDate,
                        stateChangeEmployeeId: conversation.StateChangeEmployeeId,
                        vendorType: conversation.VendorType.ToString(),
                        practiceId: conversation.PracticeId,
                        index: conversation.Index,
                        hasMessages: conversation.HasMessages,
                        description: conversation.Description,
                        deviceTokens: deviceTokens
                ),
                eventDate: DateTime.UtcNow), cancellationToken);
        }
    }
}
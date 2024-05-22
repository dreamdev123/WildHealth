using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Devices;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.ConversationParticipant;
using WildHealth.IntegrationEvents.ConversationParticipant.Payloads;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class PublishConversationParticipantAddedEventCommandHandler : IRequestHandler<PublishConversationParticipantAddedEventCommand>
{
    private readonly IEventBus _eventBus;
    private readonly IDevicesService _devicesService;
    private readonly ILogger<PublishConversationParticipantAddedEventCommandHandler> _logger;

    public PublishConversationParticipantAddedEventCommandHandler(
        IEventBus eventBus,
        IDevicesService devicesService,
        ILogger<PublishConversationParticipantAddedEventCommandHandler> logger)
    {
        _eventBus = eventBus;
        _devicesService = devicesService;
        _logger = logger;
    }

    public async Task Handle(PublishConversationParticipantAddedEventCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Publishing of conversation participant added event for {command.ConversationId}: started");
        var devices = await _devicesService.GetConversationDevices(command.ConversationSid, null); 
        var deviceTokens = devices.Select(d => d.DeviceToken).Distinct().ToArray();
            
        var payload = new ConversationParticipantAddedPayload(
            command.ConversationSid,
            command.ConversationId,
            command.Subject,
            (int) command.State,
            command.ParticipantSid,
            command.ParticipantUniversalId,
            deviceTokens);
            
        var participantAddedEvent = new ConversationParticipantIntegrationEvent(
            payload: payload,
            user: new UserMetadataModel(command.ParticipantUniversalId),
            eventDate: DateTime.UtcNow);

        await _eventBus.Publish(participantAddedEvent);
        
        _logger.LogInformation($"Publishing of conversation participant added event for {command.ConversationId}: finished");
    }
}
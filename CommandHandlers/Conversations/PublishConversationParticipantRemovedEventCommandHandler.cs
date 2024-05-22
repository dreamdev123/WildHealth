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

public class PublishConversationParticipantRemovedEventCommandHandler : IRequestHandler<PublishConversationParticipantRemovedEventCommand>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<PublishConversationParticipantRemovedEventCommandHandler> _logger;
    private readonly IDevicesService _devicesService;

    public PublishConversationParticipantRemovedEventCommandHandler(
        IEventBus eventBus,
        ILogger<PublishConversationParticipantRemovedEventCommandHandler> logger,
        IDevicesService devicesService)
    {
        _eventBus = eventBus;
        _logger = logger;
        _devicesService = devicesService;
    }

    public async Task Handle(PublishConversationParticipantRemovedEventCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Publishing of conversation participant removed event for {command.ConversationId}: started");
        var devices = await _devicesService.GetConversationDevices(command.ConversationSid, null); 
        var deviceTokens = devices.Select(d => d.DeviceToken).Distinct().ToArray();
            
        var payload = new ConversationParticipantRemovedPayload(
            command.ConversationSid,
            command.ConversationId,
            command.Subject,
            (int) command.State,
            command.ParticipantSid,
            command.ParticipantUniversalId,
            deviceTokens);
            
        var participantRemovedEvent = new ConversationParticipantIntegrationEvent(
            payload: payload,
            user: new UserMetadataModel(command.ParticipantUniversalId),
            eventDate: DateTime.UtcNow);

        await _eventBus.Publish(participantRemovedEvent);
        
        _logger.LogInformation($"Publishing of conversation participant removed event for {command.ConversationId}: finished");
    }
}
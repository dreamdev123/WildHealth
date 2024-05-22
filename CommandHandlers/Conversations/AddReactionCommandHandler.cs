using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Common.Options;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Application.Utils.Locker;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.AiAnalytics;
using WildHealth.IntegrationEvents.AiAnalytics.Payloads;
using WildHealth.IntegrationEvents._Base;
using WildHealth.Twilio.Clients.Enums;
using Microsoft.Extensions.Options;
using WildHealth.Settings;
using Newtonsoft.Json;
using MediatR;
using Polly;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class AddReactionCommandHandler : MessagingBaseService, IRequestHandler<AddReactionCommand, Unit>
{
    private readonly IConversationsService _conversationsService;
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventBus _eventBus;
    private readonly ILocker _locker;
    private readonly IOptions<PracticeOptions> _practiceOptions;
    
    public AddReactionCommandHandler(
        IConversationsService conversationsService,
        ITwilioWebClient twilioWebClient,
        ISettingsManager settingsManager, 
        IDateTimeProvider dateTimeProvider,
        IEventBus eventBus,
        ILocker locker,
        IOptions<PracticeOptions> practiceOptions) : base(settingsManager)
    {
        _conversationsService = conversationsService;
        _twilioWebClient = twilioWebClient;
        _dateTimeProvider = dateTimeProvider;
        _eventBus = eventBus;
        _locker = locker;
        _practiceOptions = practiceOptions;
    }

    public async Task<Unit> Handle(AddReactionCommand command, CancellationToken cancellationToken)
    {
        var credentials = await GetMessagingCredentialsAsync(_practiceOptions.Value.WildHealth);

        _twilioWebClient.Initialize(credentials);
        
        var policy = Policy
            .Handle<AppException>(x => x.StatusCode == HttpStatusCode.Locked)
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3)
            });

        await policy.ExecuteAsync(
            async () => await AddAttributeAsync(command)
        );
        
        return Unit.Value;
    }
    
    #region private

    private async Task AddAttributeAsync(AddReactionCommand command)
    {
        var conversation = await _conversationsService.GetByExternalVendorIdAsync(command.ConversationId);
        
        var message = await _twilioWebClient.GetMessageAsync(
            conversationId: command.ConversationId,
            id: command.MessageId
        );
        
        var isLocked = await _locker.LockAsync(message.Sid);
        
        try
        {
            var attributes = message.GetAttributes();

            var reaction = new MessageReactionModel
            {
                Id = Guid.NewGuid().ToString(),
                Type = command.Type,
                CreatedBy = command.ParticipantId,
                CreatedAt = _dateTimeProvider.UtcNow()
            };
        
            attributes.Reactions.Add(reaction);

            message.AttributesString = JsonConvert.SerializeObject(attributes);

            await _twilioWebClient.UpdateAttributes(message);

            await AddAnalyticsAsync(command, conversation, message);
        }
        finally
        {
            if (isLocked)
            {
                await _locker.UnlockAsync(message.Sid);
            }
        }
    }

    private async Task AddAnalyticsAsync(AddReactionCommand command, Conversation conversation, ConversationMessageModel message)
    {
        var domain = ConversationDomain.Create(conversation);
        
        if (!domain.IsPlaygroundConversation())
        {
            return;
        }

        var user = domain.GetConversationOwner().User;
        
        var interaction = message.GetAttributes().Interactions.First(x => x.Type == InteractionType.Recommendation);

        await _eventBus.Publish(new AiAnalyticsIntegrationEvent(
            eventDate: _dateTimeProvider.UtcNow(),
            user: new UserMetadataModel(user.UniversalId.ToString()),
            payload: new LlmResponseReactionPayload
            {
                Action = command.Type switch
                {
                    ReactionType.Like => "like",
                    ReactionType.Dislike => "dislike",
                    _ => command.Type.ToString().ToLower()
                },
                Query = message.Body,
                Response = interaction.Detail
            }));
    }
    
    #endregion
}
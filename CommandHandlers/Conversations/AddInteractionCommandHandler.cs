using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Utils.Locker;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.AiAnalytics;
using WildHealth.IntegrationEvents.AiAnalytics.Payloads;
using WildHealth.IntegrationEvents._Base;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Shared.Exceptions;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Twilio.Clients.Enums;
using WildHealth.Settings;
using Newtonsoft.Json;
using MediatR;
using Polly;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class AddInteractionCommandHandler : MessagingBaseService,  IRequestHandler<AddInteractionCommand, Unit>
{
    private readonly IConversationsService _conversationsService;
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILocker _locker;
    private readonly IOptions<PracticeOptions> _practiceOptions;
    private readonly IEventBus _eventBus;
    
    public AddInteractionCommandHandler(
        IConversationsService conversationsService,
        ISettingsManager settingsManager, 
        ITwilioWebClient twilioWebClient, 
        IDateTimeProvider dateTimeProvider, 
        ILocker locker,
        IOptions<PracticeOptions> practiceOptions,
        IEventBus eventBus) : base(settingsManager)
    {
        _conversationsService = conversationsService;
        _twilioWebClient = twilioWebClient;
        _dateTimeProvider = dateTimeProvider;
        _locker = locker;
        _practiceOptions = practiceOptions;
        _eventBus = eventBus;
    }

    public async Task<Unit> Handle(AddInteractionCommand command, CancellationToken cancellationToken)
    {
        var credentials = await GetMessagingCredentialsAsync(_practiceOptions.Value.WildHealth);

        _twilioWebClient.Initialize(credentials);

        var conversation = await _conversationsService.GetByExternalVendorIdAsync(command.ConversationId);
        
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

        await AddAnalyticsAsync(command, conversation);

        return Unit.Value;
    }
    
    #region private
    
    private async Task AddAttributeAsync(AddInteractionCommand command)
    {
        var message = await _twilioWebClient.GetMessageAsync(
            conversationId: command.ConversationId,
            id: command.MessageId
        );
        
        var isLocked = await _locker.LockAsync(message.Sid);
        
        try
        {
            var attributes = message.GetAttributes();

            var interaction = new MessageInteractionModel
            {
                Id = Guid.NewGuid().ToString(),
                ReferenceId = command.ReferenceId,
                Type = command.Type,
                Detail = command.Detail,
                CreateBy = command.ParticipantId,
                CreatedAt = _dateTimeProvider.UtcNow()
            };
        
            attributes.Interactions.Add(interaction);

            message.AttributesString = JsonConvert.SerializeObject(attributes);

            await _twilioWebClient.UpdateAttributes(message);
        }
        finally
        {
            if (isLocked)
            {
                await _locker.UnlockAsync(message.Sid);
            }
        }
    }

    private async Task AddAnalyticsAsync(AddInteractionCommand command, Conversation conversation)
    {
        switch (command.Type)
        {
            case InteractionType.RecommendationAccepted:
            case InteractionType.RecommendationEdited:
            case InteractionType.RecommendationRejected:
                await _eventBus.Publish(new AiAnalyticsIntegrationEvent(
                    payload: new AiResponseAnnotatedPayload(
                        assistanceRequestId: command.ReferenceId,
                        responseSentByUniversalId: command.UserUniversalId,
                        responseSent: WasResponseSent(command.Type),
                        responseContent: command.Detail,
                        responseStatus: command.Type.ToString(),
                        responseSentAt: DateTime.UtcNow
                    ),
                    user: new UserMetadataModel(command.UserUniversalId),
                    eventDate: DateTime.UtcNow
                ));
                break;
            case InteractionType.Recommendation:
                var domain = ConversationDomain.Create(conversation);
                if (domain.IsPlaygroundConversation())
                {
                    var user = domain.GetConversationOwner().User;
                    await _eventBus.Publish(new AiAnalyticsIntegrationEvent(
                        eventDate: DateTime.UtcNow,
                        user: new UserMetadataModel(user.UniversalId.ToString()),
                        payload: new LlmResponseGeneratedPayload
                        {
                            Flowtype = conversation.Type switch
                            {
                                ConversationType.GenericPlayground => "general_llm_playground",
                                ConversationType.PatientPlayground => "patient_llm_playground",
                                _ => throw new ArgumentException("Unsupported conversation type")
                            },
                            Response = command.Detail
                        }
                    ));
                }
                break;
            default:
                break;
        }
    }

    private bool WasResponseSent(InteractionType interactionType)
    {
        switch (interactionType)
        {
            case InteractionType.RecommendationAccepted:
            case InteractionType.RecommendationEdited:
                return true;
            case InteractionType.Recommendation:
            case InteractionType.RecommendationRejected:
            default:
                return false;
        }
    }
    
    #endregion
}
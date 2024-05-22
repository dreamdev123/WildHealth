using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Ai;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Common.Enums;
using WildHealth.Common.Models.Ai.FlowTypes;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Jenny.Clients.Models;
using WildHealth.Settings;
using WildHealth.Shared.Exceptions;
using WildHealth.Twilio.Clients.Enums;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.AiAnalytics;
using WildHealth.IntegrationEvents.AiAnalytics.Payloads;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class GeneratePlaygroundResponseCommandHandler : MessagingBaseService, IRequestHandler<GeneratePlaygroundResponseCommand, TextCompletionResponseModel>
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IMediator _mediator;
    private readonly IEventBus _eventBus;
    private readonly IOptions<PracticeOptions> _practiceOptions;
    private readonly ILogger _logger;

    public GeneratePlaygroundResponseCommandHandler(
        IDateTimeProvider dateTimeProvider,
        ISettingsManager settingsManager,
        ITwilioWebClient twilioWebClient,
        IMediator mediator,
        IEventBus eventBus,
        IOptions<PracticeOptions> practiceOptions,
        ILogger<GeneratePlaygroundResponseCommandHandler> logger) : base(settingsManager)
    {
        _dateTimeProvider = dateTimeProvider;
        _twilioWebClient = twilioWebClient;
        _mediator = mediator;
        _eventBus = eventBus;
        _practiceOptions = practiceOptions;
        _logger = logger;
    }
    
    public async Task<TextCompletionResponseModel> Handle(GeneratePlaygroundResponseCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Started generating AI Playground Response for message sid {command.MessageSid} in conversation sid {command.ConversationSid}");

        // Gather the sender UniversalId from the message
        var credentials = await GetMessagingCredentialsAsync(_practiceOptions.Value.WildHealth);

        _twilioWebClient.Initialize(credentials);

        var message = await _twilioWebClient.GetMessageAsync(command.MessageSid, command.ConversationSid);

        if (message is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "Message does not exist");
        }

        var universalId = !string.IsNullOrEmpty(command.UserUniversalId)
            ? command.UserUniversalId
            : message.Author;

        var flowTypeModel = new AiHealthCoachPlaygroundFlowTypeModel(
            messageSid: command.MessageSid,
            conversationSid: command.ConversationSid
        );

        var responseModel = await _mediator.Send(new TextCompletionCommand(
            userId: universalId,
            authorId: message.Author,
            flowTypeModel: flowTypeModel,
            flowType: command.FlowType
        ), cancellationToken);

        await SendAnalyticEvents(
            type: command.Type, 
            author: universalId,
            query: message.Body
        );

        if (responseModel.FlowType == FlowType.Regular)
        {
            // Add the AI response to the message
            await _mediator.Send(new AddInteractionCommand(
                conversationId: command.ConversationSid,
                messageId: command.MessageSid,
                referenceId: responseModel.Id,
                participantId: AiConstants.AiParticipantIdentifier,
                detail: responseModel.Text,
                type: InteractionType.Recommendation,
                userUniversalId: string.Empty
            ), cancellationToken); 
        }

        _logger.LogInformation($"Finished generating AI Playground response for message sid {command.MessageSid} in conversation sid {command.ConversationSid}");

        return responseModel;
    }
    
    #region private

    private Task SendAnalyticEvents(ConversationType type, string author, string query)
    {
        var @event = new AiAnalyticsIntegrationEvent(
            eventDate: _dateTimeProvider.UtcNow(),
            user: new UserMetadataModel(author),
            payload: new LlmResponseRequestedPayload
            {
                FlowType = type switch
                {
                    ConversationType.GenericPlayground => "general_llm_playground",
                    ConversationType.PatientPlayground => "patient_llm_playground",
                    _ => throw new ArgumentException("Unsupported conversation type")
                },
                Query = query,
            });
        
        return _eventBus.Publish(@event);
    }
    
    #endregion
}
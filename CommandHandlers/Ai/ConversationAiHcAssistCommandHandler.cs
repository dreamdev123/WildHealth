using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Ai;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Settings;
using WildHealth.Shared.Exceptions;
using WildHealth.Twilio.Clients.Enums;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Jenny.Clients.Models;
using MediatR;
using WildHealth.Common.Enums;
using WildHealth.Common.Models.Ai.FlowTypes;

namespace WildHealth.Application.CommandHandlers.Ai;

public class ConversationAiHcAssistCommandHandler : MessagingBaseService, IRequestHandler<ConversationAiHcAssistCommand, TextCompletionResponseModel>
{
    private readonly IPatientsService _patientsService;
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IMediator _mediator;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly IOptions<PracticeOptions> _practiceOptions;
    private readonly ILogger _logger;

    public ConversationAiHcAssistCommandHandler(
        ISettingsManager settingsManager,
        IPatientsService patientsService,
        ITwilioWebClient twilioWebClient,
        IMediator mediator,
        IFeatureFlagsService featureFlagsService,
        IOptions<PracticeOptions> practiceOptions,
        ILogger<ConversationAiHcAssistCommandHandler> logger) : base(settingsManager)
    {
        _patientsService = patientsService;
        _twilioWebClient = twilioWebClient;
        _mediator = mediator;
        _featureFlagsService = featureFlagsService;
        _practiceOptions = practiceOptions;
        _logger = logger;
    }

    public async Task<TextCompletionResponseModel> Handle(ConversationAiHcAssistCommand command, CancellationToken cancellationToken)
    {
        // Return if the feature flag is not enabled
        if(!_featureFlagsService.GetFeatureFlag(FeatureFlags.AiHcMessageAssist))
        {
            return new TextCompletionResponseModel() {
                Text = "Feature flag not enabled for response generation",
                Id = "-1"
            };
        }

        _logger.LogInformation($"Started generating AI HC assist message for message sid {command.MessageSid} in conversation sid {command.ConversationSid}");

        // Gather the sender UniversalId from the message
        var credentials = await GetMessagingCredentialsAsync(_practiceOptions.Value.WildHealth);

        _twilioWebClient.Initialize(credentials);

        var message = await _twilioWebClient.GetMessageAsync(command.MessageSid, command.ConversationSid);

        if (message is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "Message does not exist");
        }

        var universalId = message.Author;

        // Validate the UniversalId is valid and belongs to a Patient
        await AssertMessageFromPatient(universalId);

        var flowTypeModel = new AiHealthCoachAssistFlowTypeModel(
            messageSid: command.MessageSid,
            conversationSid: command.ConversationSid);

        var responseModel = await _mediator.Send(new TextCompletionCommand(
            userId: universalId,
            authorId: universalId,
            flowTypeModel: flowTypeModel,
            flowType: command.FlowType
        ), cancellationToken);

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
                userUniversalId: String.Empty
            ), cancellationToken); 
        }

        _logger.LogInformation($"Finished generating AI HC assist message for message sid {command.MessageSid} in conversation sid {command.ConversationSid}");

        return responseModel;
    }
    
    #region private

    private async Task AssertMessageFromPatient(string universalId)
    {
        try
        {
            var patient = await _patientsService.GetByUserUniversalId(Guid.Parse(universalId));

            if (patient is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Message author not a patient. Cannot generate AI assistance");
            }
        }
        catch (Exception ex)
        {
            // Likely parse error when trying to parse UniversalId to Guid
            _logger.LogError($"Error searching for Patient with Universal ID {universalId}. {ex}");
            throw new AppException(HttpStatusCode.BadRequest, "Could not verify message author is Patient. Cannot generate AI assistance");
        }
    }
    
    #endregion
}
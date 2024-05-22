using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Ai;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Exceptions;
using WildHealth.Jenny.Clients.Models;
using WildHealth.Jenny.Clients.WebClients;
using WildHealth.Shared.Exceptions;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Conversations;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.Models.Conversations.Alerts;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Ai;

public class MessageIntentCommandHandler : MessagingBaseService, IRequestHandler<MessageIntentCommand>
{
    private readonly IDictionary<string, string> _intentAndSubjectMap = new Dictionary<string, string>
    {
        {"prescription_request", "Other"},
        {"billing", "Billing"},
        {"clarity_issue", "Tech Support"},
        {"membership_question", "Plan Questions"}
    };
    
    private readonly IJennyConversationWebClient _jennyWebClient;
    private readonly IOptions<PracticeOptions> _practiceOptions;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly IPatientsService _patientsService;
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    
    public MessageIntentCommandHandler(
        IJennyConversationWebClient jennyWebClient,
        ISettingsManager settingsManager,
        IPatientsService patientsService,
        ITwilioWebClient twilioWebClient,
        IFeatureFlagsService featureFlagsService,
        IOptions<PracticeOptions> practiceOptions,
        IMediator mediator,
        ILogger<ConversationAiHcAssistCommandHandler> logger) : base(settingsManager)
    {
        _jennyWebClient = jennyWebClient;
        _patientsService = patientsService;
        _twilioWebClient = twilioWebClient;
        _featureFlagsService = featureFlagsService;
        _practiceOptions = practiceOptions;
        _mediator = mediator;
        _logger = logger;
    }
    
    public async Task Handle(MessageIntentCommand command, CancellationToken cancellationToken)
    {
        // Return if the feature flag is not enabled
        if(!_featureFlagsService.GetFeatureFlag(FeatureFlags.AiMessageIntent))
        {
            return;
        }
        
        _logger.LogInformation($"Started generating AI message intent for message sid {command.MessageSid} in conversation sid {command.ConversationSid}");
        
        await InitializeTwilioAsync();

        var message = await GetMessageAsync(command.MessageSid, command.ConversationSid);

        var patient = await GetPatientAsync(command.UniversalId);

        var intent = await _jennyWebClient.Intent(new MessageIntentRequestModel
        {
            Query = message.Body
        });

        await RaiseAlertAsync(command, patient, intent);
        
        _logger.LogInformation($"Finished generating AI message intent for message sid {command.MessageSid} in conversation sid {command.ConversationSid}");
    }

    private async Task RaiseAlertAsync(MessageIntentCommand command, Patient patient, MessageIntentResponseModel intent)
    {
        var alertedIntents = intent.Intents.Where(i => _intentAndSubjectMap.ContainsKey(i.Classification)); // Get all intents where we would create an alert
        if (!alertedIntents.Any()) // If no alerts to create, then return
        {
            _logger.LogInformation("No subject for intent {intent}. Alert was not created", string.Join(", ",intent.Intents.Select(i => i.Classification)));
            return;
        }
        var topAltertedIntent = alertedIntents.MinBy(x => x.Priority)!.Classification; // Lowest Priotity means most important intent, this is the one we want to create an alert for
        
        var createAlertCommand = new CreateMessageAlertCommand(
            messageId: command.MessageSid,
            conversationId: command.ConversationSid,
            type: MessageAlertType.TicketRequestAlert,
            data: new List<KeyValuePair<string, string>>
            {
                new (nameof(TicketRequestAlertAcceptedEvent.PatientId), patient.GetId().ToString()),
                new (nameof(TicketRequestAlertAcceptedEvent.PracticeId), patient.User.PracticeId.ToString()),
                new (nameof(TicketRequestAlertAcceptedEvent.LocationId), patient.LocationId.ToString()),
                new (nameof(TicketRequestAlertAcceptedEvent.Subject), _intentAndSubjectMap[topAltertedIntent]),
            });
            
        await _mediator.Send(createAlertCommand);
    }

    private async Task InitializeTwilioAsync()
    {
        var credentials = await GetMessagingCredentialsAsync(_practiceOptions.Value.WildHealth);

        _twilioWebClient.Initialize(credentials);
    }

    private async Task<ConversationMessageModel> GetMessageAsync(string messageId, string conversationId)
    {
        var message = await _twilioWebClient.GetMessageAsync(messageId, conversationId);

        if (message is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "Message does not exist");
        }

        return message;
    }
    
    private async Task<Patient> GetPatientAsync(string universalId)
    {
        try
        {
            var patient = await _patientsService.GetByUserUniversalId(Guid.Parse(universalId));

            if (patient is null)
            {
                throw new DomainException("Message author not a patient. Cannot generate AI assistance");
            }

            return patient;
        }
        catch (Exception ex)
        {
            // Likely parse error when trying to parse UniversalId to Guid
            _logger.LogError($"Error searching for Patient with Universal ID {universalId}. {ex}");
            
            throw new DomainException("Could not verify message author is Patient. Cannot generate AI assistance");
        }
    }
}
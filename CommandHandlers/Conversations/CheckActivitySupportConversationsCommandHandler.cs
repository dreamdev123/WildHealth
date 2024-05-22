using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Common.Options;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Common.Constants;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.WebClient;
using MediatR;
using WildHealth.Application.Utils.BackgroundJobs.EmployeeProvider;
using WildHealth.Domain.Constants;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class CheckActivitySupportConversationsCommandHandler : MessagingBaseService, IRequestHandler<CheckActivitySupportConversationsCommand>
    {
        
        private readonly IConversationsService _conversationsService;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IOptions<SchedulerOptions> _schedulerOptions;
        private readonly IBackgroundJobEmployeeProvider _employeeProvider;

        public CheckActivitySupportConversationsCommandHandler(
            ILogger<CheckActivitySupportConversationsCommandHandler> logger,
            IConversationsService conversationsService,
            IFeatureFlagsService featureFlagsService,
            ISettingsManager settingsManager,
            ITwilioWebClient twilioWebClient,
            IOptions<SchedulerOptions> schedulerOptions,
            IMediator mediator,
            IBackgroundJobEmployeeProvider employeeProvider) : base(settingsManager)
        {
            _conversationsService = conversationsService;
            _featureFlagsService = featureFlagsService;
            _twilioWebClient = twilioWebClient;
            _mediator = mediator;
            _logger = logger;
            _schedulerOptions = schedulerOptions;
            _employeeProvider = employeeProvider;
        }

        public async Task Handle(CheckActivitySupportConversationsCommand request, CancellationToken cancellationToken)
        {
            if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.ConversationsBackgroundJobs))
            {
                return;
            }

            bool IsOverTwoWeeks(DateTime date) => DateTime.UtcNow.Subtract(date).TotalDays >= _schedulerOptions.Value.CloseSupportConversationsWithNoActivityInDays;
            
            var conversations = (await _conversationsService.GetAllActiveSupportAsync()).Where(o => !String.IsNullOrEmpty(o.VendorExternalId));

            foreach (var conversation in conversations)
            {
                try
                {
                    var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

                    _twilioWebClient.Initialize(credentials);

                    var messagesFromConversation = await _twilioWebClient.GetMessagesAsync(conversation.VendorExternalId, MessagesOrderType.desc.ToString(), 1);

                    var lastMessage = messagesFromConversation.Messages.FirstOrDefault();

                    if (lastMessage is null)
                    {
                        if (IsOverTwoWeeks(conversation.StartDate))
                        {
                            var employee = await _employeeProvider.GetBackgroundJobEmployee();
                            await _mediator.Send(new UpdateStateConversationCommand(
                                    conversationId: conversation.GetId(), 
                                    conversationState: ConversationState.Closed,
                                    employeeId: employee.GetId()),
                                cancellationToken);
                            
                            _logger.LogInformation($"Conversation with id {conversation.Id} is closed on the 14th day after starting without messages by background job.");
                        }

                        continue;
                    }

                    if (IsOverTwoWeeks(lastMessage.DateUpdated))
                    {
                        var employee = await _employeeProvider.GetBackgroundJobEmployee();
                        await _mediator.Send(new UpdateStateConversationCommand(
                                conversationId: conversation.GetId(), 
                                conversationState: ConversationState.Closed,
                                employeeId: employee.GetId()),
                            cancellationToken);
                        
                        _logger.LogInformation($"Conversation with id {conversation.Id} is closed on the 14th day after last message by background job.");
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Closing inactive conversation support tickets was failed. with Error: {ex.ToString()}");
                }
            }
        }
    }
}
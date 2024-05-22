using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Services.Messaging.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Shared.Exceptions;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class RecoverParticipantExternalIdCommandHandler : MessagingBaseService, IRequestHandler<RecoverParticipantExternalIdCommand, ConversationParticipantEmployee?>
    {
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IConversationsService _conversationsService;
        
        private readonly IServiceProvider _services;
        private readonly IMediator _mediator;
        private readonly ILogger<RecoverParticipantExternalIdCommandHandler> _logger;

        public RecoverParticipantExternalIdCommandHandler(
            ITwilioWebClient twilioWebClient,
            IConversationsService conversationsService,
            IServiceProvider services,
            IMediator mediator,
            ISettingsManager settingsManager,
            ILogger<RecoverParticipantExternalIdCommandHandler> logger) : base(settingsManager)
        {
            _twilioWebClient = twilioWebClient;
            _conversationsService = conversationsService;
            _services = services;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<ConversationParticipantEmployee?> Handle(RecoverParticipantExternalIdCommand command, CancellationToken cancellationToken)
        {
            var conversation = await _conversationsService.GetByExternalVendorIdAsync(command.ConversationSid, true);

            var targetParticipant =
                conversation.EmployeeParticipants.FirstOrDefault(o => o.VendorExternalIdentity == command.Identity);

            if (targetParticipant is null)
            {
                _logger.LogInformation($"Unable to locate participant id for [ConversationSid] = {command.ConversationSid}, [Identity] = {command.Identity}");
                
                return null;
            }
            
            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

            _twilioWebClient.Initialize(credentials);

            var participantResources = await _twilioWebClient.GetConversationParticipantResourcesAsync(command.ConversationSid);

            var targetParticipantInTwilio =
                participantResources.Participants.FirstOrDefault(o => o.Identity == command.Identity);

            if (targetParticipantInTwilio is null)
            {
                _logger.LogInformation($"Unable to recover participant id for [ConversationSid] = {command.ConversationSid}, [Identity] = {command.Identity}");
                
                return null;
            }
            
            targetParticipant.SetVendorExternalId(targetParticipantInTwilio.Sid);

            await _conversationsService.UpdateConversationAsync(conversation);

            return targetParticipant;
        }
    }
}

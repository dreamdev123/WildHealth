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
using WildHealth.Twilio.Clients.Exceptions;
using WildHealth.Twilio.Clients.Models.ConversationParticipants;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class RemoveOrphanedParticipantsCommandHandler : MessagingBaseService, IRequestHandler<RemoveOrphanedParticipantsCommand>
    {
        private readonly IMessagingConversationService _messagingConversationService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IConversationsService _conversationsService;
        
        private readonly IServiceProvider _services;
        private readonly IMediator _mediator;
        private readonly ILogger<RemoveOrphanedParticipantsCommandHandler> _logger;

        public RemoveOrphanedParticipantsCommandHandler(
            IMessagingConversationService messagingConversationService,
            ITwilioWebClient twilioWebClient,
            IConversationsService conversationsService,
            IServiceProvider services,
            IMediator mediator,
            ISettingsManager settingsManager,
            ILogger<RemoveOrphanedParticipantsCommandHandler> logger) : base(settingsManager)
        {
            _messagingConversationService = messagingConversationService;
            _twilioWebClient = twilioWebClient;
            _conversationsService = conversationsService;
            _services = services;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(RemoveOrphanedParticipantsCommand command, CancellationToken cancellationToken)
        {
            var conversation = await _conversationsService.GetByExternalVendorIdAsync(command.ConversationSid, true);

            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

            _twilioWebClient.Initialize(credentials);

            var participantResources = await _twilioWebClient.GetConversationParticipantResourcesAsync(command.ConversationSid);

            foreach (var participant in participantResources.Participants)
            {
                var (senderId, participantUniversalId, isSuccess) = await _mediator.Send(new GetConversationParticipantIdentityCommand(
                    conversationId: conversation.GetId(),
                    participantSid: participant.Sid));

                // If the participant doesn't exist in the database and
                // it was created before June 1
                if (!isSuccess && participant.DateCreated <= DateTime.Parse("2022-06-01"))
                {
                    await _twilioWebClient.RemoveConversationParticipantAsync(
                        conversation: new ConversationModel()
                        {
                            Sid = command.ConversationSid
                        },
                        model: new CreateConversationParticipantModel()
                        {
                            Identity = participant.Identity
                        });
                }
            }

            var twilioParticipantIdentities = participantResources.Participants.Select(o => o.Identity);
            
            // Also find all cases where our DB says they should be in the conversation but they are not, then we want to "delete" them on the DB
            foreach (var participant in conversation.EmployeeParticipants.Where(o => 
                         !twilioParticipantIdentities.Contains(o.VendorExternalIdentity) &&
                         o.IsPresent
                         ))
            {
                await _mediator.Send(new RemoveEmployeeParticipantFromConversationCommand(
                    conversationId: conversation.GetId(),
                    userId: participant.Employee.UserId
                ));
            }
        }
    }
}

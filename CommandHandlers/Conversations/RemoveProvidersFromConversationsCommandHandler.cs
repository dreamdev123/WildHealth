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
using WildHealth.Domain.Comparers.Conversations;
using WildHealth.Domain.Constants;
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
    public class RemoveProvidersFromConversationsCommandHandler : MessagingBaseService, IRequestHandler<RemoveProvidersFromConversationsCommand>
    {
        private readonly IMessagingConversationService _messagingConversationService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IConversationsService _conversationsService;
        
        private readonly IServiceProvider _services;
        private readonly IMediator _mediator;
        private readonly ILogger<RemoveProvidersFromConversationsCommandHandler> _logger;

        public RemoveProvidersFromConversationsCommandHandler(
            IMessagingConversationService messagingConversationService,
            ITwilioWebClient twilioWebClient,
            IConversationsService conversationsService,
            IServiceProvider services,
            IMediator mediator,
            ISettingsManager settingsManager,
            ILogger<RemoveProvidersFromConversationsCommandHandler> logger) : base(settingsManager)
        {
            _messagingConversationService = messagingConversationService;
            _twilioWebClient = twilioWebClient;
            _conversationsService = conversationsService;
            _services = services;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(RemoveProvidersFromConversationsCommand command, CancellationToken cancellationToken)
        {
            var patientCancelledConversations = await _conversationsService.HealthConversationsWithProviderAndPatientCancelled();
            var staleHealthConversations =
                await _conversationsService.HealthConversationsWithProviderStaleForDays(command
                    .HealthConversationStaleForDays);
            var staleSupportConversations =
                await _conversationsService.SupportConversationsWithProviderStaleForDays(command
                    .SupportConversationStaleForDays);

            var allConversations = patientCancelledConversations.Concat(staleHealthConversations)
                .Concat(staleSupportConversations)
                .Distinct(ConversationEqualityComparer.Create)
                .ToArray();
            
            foreach (var conversation in allConversations)
            {
                // get employee provider participant and remove them
                foreach (var participant in conversation
                             .EmployeeParticipants
                             .Where(o => o.Employee.RoleId == Roles.ProviderId))
                {
                    await _mediator.Send(
                        new RemoveEmployeeParticipantFromConversationCommand(conversation.GetId(),
                            participant.Employee.UserId));
                }
            }
        }
    }
}

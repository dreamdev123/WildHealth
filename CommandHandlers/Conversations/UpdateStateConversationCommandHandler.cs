using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Settings;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class UpdateStateConversationCommandHandler : MessagingBaseService, IRequestHandler<UpdateStateConversationCommand, Conversation>
    {
        private readonly ILogger _logger;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IMediator _mediator;
        private readonly IConversationsService _conversationsService;
        private readonly ITransactionManager _transactionManager;

        public UpdateStateConversationCommandHandler(
            ILogger<UpdateStateConversationCommandHandler> logger,
            ITwilioWebClient twilioWebClient,
            ISettingsManager settingsManager,
            IMediator mediator,
            IConversationsService conversationsService,
            ITransactionManager transactionManager) : base(settingsManager)
        {
            _logger = logger;
            _twilioWebClient = twilioWebClient;
            _mediator = mediator;
            _conversationsService = conversationsService;
            _transactionManager = transactionManager;
        }


        public async Task<Conversation> Handle(UpdateStateConversationCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Updating Conversation with [ConversationId] {command.ConversationId} has been started.");

            var conversation = await _conversationsService.GetByIdAsync(command.ConversationId);
            var conversationDomain = ConversationDomain.Create(conversation);

            await using var transaction = _transactionManager.BeginTransaction();

            try
            {
                conversationDomain.SetState(command.ConversationState, command.StateChangeEmployeeId);

                await _conversationsService.UpdateConversationAsync(conversation);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }

            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

            _twilioWebClient.Initialize(credentials);

            var twilioConversation = await _twilioWebClient.GetConversationAsync(id: conversation.VendorExternalId);

            var newState = conversation.State.ToString().ToLower();

            // If it's a confirmed state change for the conversation at Twilio, then we want to reach out and make updates there
            if (newState != twilioConversation.State)
            {
                // When a conversation is closed, and it wasn't already closed, then mark all of the messages as read
                if (conversation.State == ConversationState.Closed)
                {
                    await _mediator.Send(new MarkAllConversationMessagesReadCommand(
                        conversationId: conversation.GetId()
                    ));
                }
                
                // Set the new state
                await _twilioWebClient.UpdateConversationAsync(new ConversationModel
                {
                    Sid = conversation.VendorExternalId,
                    State = newState
                });
            }

            var conversationUpdatedEvent = new ConversationUpdatedEvent(conversation);

            await _mediator.Publish(conversationUpdatedEvent, cancellationToken);

            _logger.LogInformation($"Updating Conversation with [ConversationId] {command.ConversationId} has been finished.");

            return conversation;
        }
    }
}

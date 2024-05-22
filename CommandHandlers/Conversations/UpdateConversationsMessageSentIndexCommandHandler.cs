using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Conversations.Flows;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Conversations;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class UpdateConversationsMessageSentIndexCommandHandler : IRequestHandler<UpdateConversationsMessageSentIndexCommand>
    {
        private readonly IConversationParticipantMessageSentIndexService _conversationParticipantMessageSentIndexService;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly MaterializeFlow _materializeFlow;

        public UpdateConversationsMessageSentIndexCommandHandler(
            IConversationParticipantMessageSentIndexService conversationParticipantMessageSentIndexService,
            ILogger<UpdateConversationsMessageSentIndexCommandHandler> logger,
            IMediator mediator, 
            MaterializeFlow materializeFlow)
        {
            _conversationParticipantMessageSentIndexService = conversationParticipantMessageSentIndexService;
            _logger = logger;
            _mediator = mediator;
            _materializeFlow = materializeFlow;
        }

        public async Task Handle(UpdateConversationsMessageSentIndexCommand command,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"[UpdateConversationsMessageSentIndexCommandHandler] Started updating conversation messages sent indexes [senderId]: {command.ParticipantVendorExternalId} and [conversationID]: {command.ConversationVendorExternalId}");

            try
            {
                var (_, participantUniversalId, isSuccess) = await _mediator.Send(new GetConversationParticipantIdentityCommand(
                    conversationId: command.ConversationId,
                    participantSid: command.ParticipantVendorExternalId), cancellationToken);
                
                if (isSuccess)
                {
                    var messageSentIndex = await _conversationParticipantMessageSentIndexService.GetByConversationAndParticipantAsync(
                        command.ConversationVendorExternalId, 
                        command.ParticipantVendorExternalId);

                    await new BumpMessageSentIndexFlow(
                        messageSentIndex, 
                        command.ConversationId,
                        command.ConversationVendorExternalId,
                        command.ParticipantVendorExternalId, 
                        command.CreatedAt,
                        command.Index, 
                        participantUniversalId).Materialize(_materializeFlow);
                }
                
                _logger.LogInformation($"[UpdateConversationsMessageSentIndexCommandHandler] Finished updating conversation messages sent indexes [senderId]: {command.ParticipantVendorExternalId} and [conversationID]: {command.ConversationVendorExternalId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"[UpdateConversationsMessageSentIndexCommandHandler] Error during updating conversation message sent index with [convesartion id] {command.ConversationVendorExternalId} index: {command.Index} with [Error]: {ex.ToString()}");
                throw new ApplicationException(
                    $"[UpdateConversationsMessageSentIndexCommandHandler] Error during updating conversation message sent index with [convesartion id] {command.ConversationVendorExternalId} index: {command.Index}",
                    ex);
            }
        }
    }
}
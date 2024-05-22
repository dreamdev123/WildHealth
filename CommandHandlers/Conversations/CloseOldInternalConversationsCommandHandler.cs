using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Shared.Data.Repository;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Enums.Conversations;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class CloseOldInternalConversationsCommandHandlerHandler : IRequestHandler<CloseOldInternalConversationsCommand>
    {
        private readonly ILogger<CloseOldInternalConversationsCommandHandlerHandler> _logger;
        private readonly IGeneralRepository<Conversation> _conversationsRepository;
        private readonly IGeneralRepository<ConversationParticipantEmployee> _conversationParticipantEmployeesRepository;
        private readonly IMediator _mediator;

        public CloseOldInternalConversationsCommandHandlerHandler(
            ILogger<CloseOldInternalConversationsCommandHandlerHandler> logger,
            IGeneralRepository<Conversation> conversationsRepository,
            IGeneralRepository<ConversationParticipantEmployee> conversationParticipantEmployeesRepository,
            IMediator mediator)
        {
            _logger = logger;
            _conversationsRepository = conversationsRepository;
            _conversationParticipantEmployeesRepository = conversationParticipantEmployeesRepository;
            _mediator = mediator;
        }

        public async Task Handle(CloseOldInternalConversationsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting to clean old internal conversations older than {command.ConversationsOlderThanDays} days.");

            var unsignedParticipantEmployees = await _conversationParticipantEmployeesRepository
                .All()
                .Include(o => o.Conversation)
                .Where(o => o.Conversation.CreatedAt <= DateTime.UtcNow.AddDays(-command.ConversationsOlderThanDays))
                .Where(o => o.Conversation.State == ConversationState.Active)
                .Where(o => o.Conversation.Type == ConversationType.Internal)
                .Where(o => !o.IsSigned)
                .ToArrayAsync(cancellationToken: cancellationToken);

            _logger.LogInformation($"Found [ConversationParticipantEmployeeCount] = {unsignedParticipantEmployees.Count()}");

            foreach(var unsignedParticipantEmployee in unsignedParticipantEmployees)
            {
                await _mediator.Send(new SignOffConversationCommand(
                    conversationId: unsignedParticipantEmployee.ConversationId,
                    employeeId: unsignedParticipantEmployee.EmployeeId
                ));

                _logger.LogInformation($"Running for [ConversationId] = {unsignedParticipantEmployee.ConversationId}, [EmployeeId] = {unsignedParticipantEmployee.EmployeeId}");
            }

            _logger.LogInformation($"Starting to clean old internal conversations older than {command.ConversationsOlderThanDays} days.");
        }
    }
}
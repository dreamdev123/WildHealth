using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Entities.Conversations;
using MediatR;
using System.Linq;
using System.Net;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Enums.Conversations;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class SignOffConversationCommandHandler : IRequestHandler<SignOffConversationCommand, Conversation>
    {
        private readonly IConversationsService _conversationsService;
        private readonly IMediator _mediator;

        public SignOffConversationCommandHandler(
            IConversationsService conversationsService,
            IMediator mediator)
        {
            _conversationsService = conversationsService;
            _mediator = mediator;
        }

        public async Task<Conversation> Handle(SignOffConversationCommand command, CancellationToken cancellationToken)
        {
            var conversation = await _conversationsService.GetByIdAsync(command.ConversationId);

            var employee = conversation.EmployeeParticipants.FirstOrDefault(x => x.EmployeeId == command.EmployeeId);

            if (employee is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Employee does not exist in the conversation.");
            }

            employee.SignOff();

            await _conversationsService.UpdateConversationAsync(conversation);

            await _mediator.Send(new CheckConversationSignOffCommand(conversationId: conversation.GetId()),
                cancellationToken);

            return conversation;
        }
    }
}

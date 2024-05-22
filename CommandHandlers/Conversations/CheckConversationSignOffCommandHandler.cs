using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Enums.Conversations;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class CheckConversationSignOffCommandHandler : IRequestHandler<CheckConversationSignOffCommand, Unit>
{
    private readonly IConversationsService _conversationsService;
    private readonly ILogger<CheckConversationSignOffCommandHandler> _logger;
    private readonly IMediator _mediator;

    public CheckConversationSignOffCommandHandler(
        IConversationsService conversationsService,
        ILogger<CheckConversationSignOffCommandHandler> logger,
        IMediator mediator)
    {
        _conversationsService = conversationsService;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(CheckConversationSignOffCommand command, CancellationToken cancellationToken)
    {
        var conversation = await _conversationsService.GetByIdAsync(command.ConversationId);

        // if all participants are either not present or signed then all required have signed
        var areAllPresentSigned = conversation.EmployeeParticipants.All(i => !i.IsPresent || i.IsSigned);

        if (areAllPresentSigned && conversation.Type == ConversationType.Internal)
        {
            var lastSigned = conversation
                .EmployeeParticipants
                .Where(x => x.IsSigned)
                .MaxBy(x => x.SignDate);

            if (lastSigned is null)
                return Unit.Value;
            
            await _mediator.Send(new UpdateStateConversationCommand(conversation.GetId(), ConversationState.Closed, lastSigned.EmployeeId), cancellationToken);
        }

        return Unit.Value;
    }
}
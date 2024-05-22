using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Entities.Conversations;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class UpdateConversationSubjectCommandHandler : IRequestHandler<UpdateConversationSubjectCommand, Conversation>
{
    private readonly IConversationsService _conversationsService;

    public UpdateConversationSubjectCommandHandler(IConversationsService conversationsService)
    {
        _conversationsService = conversationsService;
    }

    public async Task<Conversation> Handle(UpdateConversationSubjectCommand command, CancellationToken cancellationToken)
    {
        var conversation = await _conversationsService.GetByIdAsync(command.Id);

        conversation.Subject = command.NewSubject;

        await _conversationsService.UpdateConversationAsync(conversation);

        return conversation;
    }
}
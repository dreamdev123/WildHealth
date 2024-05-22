using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Models.Conversation;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class MessageAddedCommandHandler : IRequestHandler<MessageAddedCommand>
    {
        private readonly IConversationsService _conversationsService;

        public MessageAddedCommandHandler(IConversationsService conversationsService) 
        { 
            _conversationsService = conversationsService;
        }

        /// <summary>
        /// This handler will send clarity notification when new message will be sent if conversation is not opened
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Handle(MessageAddedCommand request, CancellationToken cancellationToken)
        {
            var conversation = await _conversationsService.GetByExternalVendorIdAsync(request.ConversationSid);
            var conversationDomain = ConversationDomain.Create(conversation);

            conversationDomain.SetIndex(request.Index);
            conversationDomain.SetHasMessages(true);
            
            await _conversationsService.UpdateConversationAsync(conversation);
        }
    }
}

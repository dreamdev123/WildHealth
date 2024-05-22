using MediatR;
using System;

namespace WildHealth.Application.Commands.Conversations
{
    public class CheckUnreadMessagesInAllConversationsCommand : IRequest
    {
        public DateTime? Runtime { get; protected set; }
        public CheckUnreadMessagesInAllConversationsCommand(DateTime? runtime) 
        {
            this.Runtime = runtime;
        }
    }
}
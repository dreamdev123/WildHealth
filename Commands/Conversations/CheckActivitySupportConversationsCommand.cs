using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// Command provides that support conversation will close after 14 days inactivity
    /// </summary>
    public class CheckActivitySupportConversationsCommand:IRequest
    {
        public CheckActivitySupportConversationsCommand() { }
    }
}

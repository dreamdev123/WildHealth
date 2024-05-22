using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// Command provide sending scheduled messages if the time has come
    /// </summary>
    public class SendScheduledMessagesCommand : IRequest
    {
        public SendScheduledMessagesCommand(){ }
    }
}
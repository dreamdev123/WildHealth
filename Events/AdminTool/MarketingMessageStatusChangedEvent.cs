using MediatR;
using WildHealth.Domain.Enums.Messages;

namespace WildHealth.Application.Events.AdminTool;

public class MarketingMessageStatusChangedEvent: INotification
{
    public int MessageId { get; }

    public MessageStatus MessageStatus { get; }

    public MarketingMessageStatusChangedEvent(int messageId, MessageStatus messageStatus)
    {
        MessageId = messageId;
        MessageStatus = messageStatus;
    }
}
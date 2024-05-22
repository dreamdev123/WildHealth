using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.AdminTool;
using WildHealth.Application.Services.InternalMessagesService;

namespace WildHealth.Application.EventHandlers.AdminTool;

public class MarketingMessageStatusChangedEventHandler: INotificationHandler<MarketingMessageStatusChangedEvent>
{
    private readonly IInternalMessagesService _internalMessagesService;

    public MarketingMessageStatusChangedEventHandler(IInternalMessagesService internalMessagesService)
    {
        _internalMessagesService = internalMessagesService;
    }


    public async Task Handle(MarketingMessageStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var message = await _internalMessagesService.GetByIdAsync(notification.MessageId);
        
        message.UpdateStatus(notification.MessageStatus);

        await _internalMessagesService.UpdateMessageAsync(message);
    }
}
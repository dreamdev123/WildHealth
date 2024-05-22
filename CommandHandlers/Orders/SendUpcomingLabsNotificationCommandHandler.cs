using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Notifications;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;

namespace WildHealth.Application.CommandHandlers.Orders;

public class SendUpcomingLabsNotificationCommandHandler : IRequestHandler<SendUpcomingLabsNotificationCommand>
{
    private const int DaysOffset = 14;
    
    private readonly ILabOrdersService _labOrdersService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationsService;

    public SendUpcomingLabsNotificationCommandHandler(ILabOrdersService labOrdersService, IDateTimeProvider dateTimeProvider, INotificationService notificationsService)
    {
        _labOrdersService = labOrdersService;
        _dateTimeProvider = dateTimeProvider;
        _notificationsService = notificationsService;
    }

    public async Task Handle(SendUpcomingLabsNotificationCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow();
        var expectedCollectionDate = utcNow.AddDays(DaysOffset);
        var orders = await _labOrdersService.GetByExpectedCollectionDate(expectedCollectionDate);

        foreach (var order in orders)
        {
            var notification = new UpcomingLabsNotification(order);
            
            await _notificationsService.CreateNotificationAsync(notification);
        }
    }
}
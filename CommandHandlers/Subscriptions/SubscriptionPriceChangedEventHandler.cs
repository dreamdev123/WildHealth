using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Domain.Models.Payment;

namespace WildHealth.Application.CommandHandlers.Subscriptions;

public record SubscriptionPriceChangedEvent(int PracticeId, string SubscriptionId, SubscriptionPriceDomain SubscriptionPrice) : INotification;

public class SubscriptionPriceChangedEventHandler : INotificationHandler<SubscriptionPriceChangedEvent>
{
    private readonly IPaymentService _paymentService;

    public SubscriptionPriceChangedEventHandler(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task Handle(SubscriptionPriceChangedEvent notification, CancellationToken cancellationToken)
    {
        await _paymentService.UpdateSubscriptionPriceAsync(notification.PracticeId, notification.SubscriptionId, notification.SubscriptionPrice);
    }
}
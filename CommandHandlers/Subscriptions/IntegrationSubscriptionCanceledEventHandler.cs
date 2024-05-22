using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.CommandHandlers.Subscriptions;

public record IntegrationSubscriptionCanceledEvent(Patient Patient,
    Subscription ClaritySubscription,
    PaymentPrice PaymentPrice,
    EmployerProduct? EmployerProduct,
    PromoCodeCoupon? PromoCodeCoupon, 
    IntegrationVendor IntegrationVendor) : INotification;

public class IntegrationSubscriptionCanceledEventHandler : INotificationHandler<IntegrationSubscriptionCanceledEvent>
{
    private readonly IPaymentService _paymentService;
    private readonly MaterializeFlow _materializer;
    
    public IntegrationSubscriptionCanceledEventHandler(
        IPaymentService paymentService, 
        MaterializeFlow materializer)
    {
        _paymentService = paymentService;
        _materializer = materializer;
    }

    public async Task Handle(IntegrationSubscriptionCanceledEvent notification, CancellationToken cancellationToken)
    {
        var originSubscription = await _paymentService.BuySubscriptionAsyncV2(notification.Patient, 
            notification.ClaritySubscription, 
            notification.PaymentPrice, 
            notification.EmployerProduct, 
            notification.PromoCodeCoupon, 
            false);

        await _paymentService.ProcessSubscriptionPaymentAsync(notification.Patient, originSubscription.Id);

        // Link Clarity subscription with subscription in the integration system
        await new MarkSubscriptionAsPaidFlow(notification.ClaritySubscription, originSubscription.Id, notification.IntegrationVendor).Materialize(_materializer);
    }
}
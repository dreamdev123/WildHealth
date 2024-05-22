using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Products;
using WildHealth.Application.Events.Subscriptions;
using MediatR;

namespace WildHealth.Application.EventHandlers.Subscriptions;

public class ExpireBuiltInProductsOnSubscriptionCanceledEvent : INotificationHandler<SubscriptionCancelledEvent>
{
    private readonly IMediator _mediator;

    public ExpireBuiltInProductsOnSubscriptionCanceledEvent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(SubscriptionCancelledEvent notification, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ExpirePatientProductsCommand(notification.Patient.GetId(), "Subscription canceled"), cancellationToken);
    }
}
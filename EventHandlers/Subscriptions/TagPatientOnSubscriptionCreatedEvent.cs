using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Tags;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Common.Constants;
using MediatR;

namespace WildHealth.Application.EventHandlers.Subscriptions;

public class TagPatientOnSubscriptionCreatedEvent : INotificationHandler<SubscriptionCreatedEvent>
{
    private readonly IMediator _mediator;

    public TagPatientOnSubscriptionCreatedEvent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(SubscriptionCreatedEvent notification, CancellationToken cancellationToken)
    {
        var command = new RemoveTagCommand(notification.Patient, Tags.NeedsActivation);
        
        await _mediator.Send(command, CancellationToken.None);
    }
}
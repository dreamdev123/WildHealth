using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Application.Services.Tags;

namespace WildHealth.Application.EventHandlers.Subscriptions;

public class TagPatientOnSubscriptionCancelledEvent : INotificationHandler<SubscriptionCancelledEvent>
{
    private readonly ITagRelationsService _tagRelationsService;

    public TagPatientOnSubscriptionCancelledEvent(ITagRelationsService tagRelationsService)
    {
        _tagRelationsService = tagRelationsService;
    }

    public async Task Handle(SubscriptionCancelledEvent notification, CancellationToken cancellationToken)
    {
        if (notification.PaymentPlan.CanBeActivated)
        {
            await _tagRelationsService.GetOrCreate(notification.Patient, Common.Constants.Tags.NeedsActivation);
        }
    }
}
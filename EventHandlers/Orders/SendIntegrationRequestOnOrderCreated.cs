using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;
using WildHealth.Domain.Enums.Orders;

namespace WildHealth.Application.EventHandlers.Orders
{
    public class SendIntegrationRequestOnOrderCreated : INotificationHandler<OrderCreatedEvent>
    {
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IMediator _mediator;

        public SendIntegrationRequestOnOrderCreated(
            IFeatureFlagsService featureFlagsService,
            IMediator mediator)
        {
            _featureFlagsService = featureFlagsService;
            _mediator = mediator;
        }

        public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
        {
            var order = notification.Order;

            if (order.Type == OrderType.Dna && order.Status != OrderStatus.ManualFlow &&  _featureFlagsService.GetFeatureFlag(FeatureFlags.DnaOrderKit))
            {
                var command = new PlaceDnaOrderCommand(order.GetId(), order.PatientId);

                await _mediator.Send(command, cancellationToken);
            }
        }
    }
}


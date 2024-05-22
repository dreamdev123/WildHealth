using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.Orders.Epigenetic;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Models.Orders;
using MediatR;
using WildHealth.Application.Domain.PaymentIssues;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class MarkEpigeneticOrderAsPaidCommandHandler : IRequestHandler<MarkEpigeneticOrderAsPaidCommand, EpigeneticOrder>
    {
        private readonly IEpigeneticOrdersService _epigeneticOrdersService;
        private readonly IPaymentIssueManager _paymentIssueManager;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public MarkEpigeneticOrderAsPaidCommandHandler(
            IEpigeneticOrdersService epigeneticOrdersService,
            IMediator mediator, 
            ILogger<MarkEpigeneticOrderAsPaidCommandHandler> logger, 
            IPaymentIssueManager paymentIssueManager)
        {
            _epigeneticOrdersService = epigeneticOrdersService;
            _mediator = mediator;
            _logger = logger;
            _paymentIssueManager = paymentIssueManager;
        }

        public async Task<EpigeneticOrder> Handle(MarkEpigeneticOrderAsPaidCommand command, CancellationToken cancellationToken)
        {
            var order = command.Order;
            var orderDomain = OrderDomain.Create(order);
            
            _logger.LogInformation($"Marking as paid of Epigenetic order with id: {order.Id} has been started.");
            
            orderDomain.MarkAsPaid(
                paymentId: command.PaymentId,
                date: command.PaymentDate
            );

            await _epigeneticOrdersService.UpdateAsync(order);
            await _paymentIssueManager.ResolveOrderPaymentIssueIfExists(order.GetId());
            await _mediator.Publish(new OrderStatusChangedEvent(order), cancellationToken);
            
            _logger.LogInformation($"Marking as paid of Epigenetic order with id: {order.Id} has been finished.");

            return order;
        }
    }
}
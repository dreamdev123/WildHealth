using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Models.Orders;
using WildHealth.Application.Events.Orders;
using Microsoft.Extensions.Logging;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class MarkDnaOrderAsPaidCommandHandler : IRequestHandler<MarkDnaOrderAsPaidCommand, DnaOrder>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public MarkDnaOrderAsPaidCommandHandler(
            IDnaOrdersService dnaOrdersService,
            IMediator mediator,
            ILogger<MarkDnaOrderAsPaidCommandHandler> logger)
        {
            _dnaOrdersService = dnaOrdersService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<DnaOrder> Handle(MarkDnaOrderAsPaidCommand command, CancellationToken cancellationToken)
        {
            var order = command.Order;
            var orderDomain = OrderDomain.Create(order);

            _logger.LogInformation($"Marking as paid of DNA order with id: {order.Id} has been started.");
            
            orderDomain.MarkAsPaid(
                paymentId: command.PaymentId,
                date: command.PaymentDate
            );

            await _dnaOrdersService.UpdateAsync(order);

            await _mediator.Publish(new OrderStatusChangedEvent(order), cancellationToken);
            
            _logger.LogInformation($"Marking as paid of DNA order with id: {order.Id} has been started.");

            return order;
        }
    }
}
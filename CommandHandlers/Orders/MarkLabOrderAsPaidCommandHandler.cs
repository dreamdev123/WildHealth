using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Models.Orders;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.Domain.Exceptions;
using MediatR;
using WildHealth.Application.Domain.PaymentIssues;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class MarkLabOrderAsPaidCommandHandler : IRequestHandler<MarkLabOrderAsPaidCommand, LabOrder>
    {
        private readonly ILabOrdersService _labOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IPaymentIssueManager _paymentIssueManager;

        public MarkLabOrderAsPaidCommandHandler(
            ILabOrdersService labOrdersService,
            IMediator mediator, 
            ILogger<MarkLabOrderAsPaidCommandHandler> logger, 
            IPaymentIssueManager paymentIssueManager)
        {
            _labOrdersService = labOrdersService;
            _mediator = mediator;
            _logger = logger;
            _paymentIssueManager = paymentIssueManager;
        }
        
        public async Task<LabOrder> Handle(MarkLabOrderAsPaidCommand command, CancellationToken cancellationToken)
        {
            var order = command.Order;
            var orderDomain = OrderDomain.Create(order);
            
            _logger.LogInformation($"Marking as paid of Lab order with id: {order.Id} has been started.");
            
            try {
                orderDomain.MarkAsPaid(
                    paymentId: command.PaymentId,
                    date: command.PaymentDate
                );

                await _labOrdersService.UpdateAsync(order);
                await _paymentIssueManager.ResolveOrderPaymentIssueIfExists(order.GetId());
                await _mediator.Publish(new OrderStatusChangedEvent(order), cancellationToken);

            } catch (DomainException ex) {
                if(ex.Message.Contains("already paid")) {
                     _logger.LogInformation($"[LabOrderId]: {order.Id} has already been marked as paid with [PaymentId] = {order.PaymentId} and will not be marked as paid with [PaymentId] = {command.PaymentId}.");

                    return order;
                }
            }
            
            _logger.LogInformation($"Marking as paid of Lab order with id: {order.Id} has been finished.");

            return order;
        }
    }
}
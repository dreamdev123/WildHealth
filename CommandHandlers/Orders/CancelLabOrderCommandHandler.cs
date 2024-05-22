using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Models.Orders;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Application.Domain.PaymentIssues;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class CancelLabOrderCommandHandler : IRequestHandler<CancelLabOrderCommand, LabOrder>
    {
        private readonly ILabOrdersService _labOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IPaymentService _paymentService;
        private readonly IEmployeeService _employeeService;
        private readonly IPaymentIssueManager _paymentIssueManager;

        public CancelLabOrderCommandHandler(
            ILabOrdersService labOrdersService, 
            IMediator mediator, 
            ILogger<CancelLabOrderCommandHandler> logger,
            IPaymentService paymentService,
            IEmployeeService employeeService, 
            IPaymentIssueManager paymentIssueManager)
        {
            _labOrdersService = labOrdersService;
            _mediator = mediator;
            _logger = logger;
            _paymentService = paymentService;
            _employeeService = employeeService;
            _paymentIssueManager = paymentIssueManager;
        }

        public async Task<LabOrder> Handle(CancelLabOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Cancelling Lab order with id: {command.Id} has been started.");
                        
            var order = await _labOrdersService.GetByIdAsync(command.Id);
            var cancelledByEmployee = await _employeeService.GetByIdAsync(command.CancelledById);

            var originalStatus = order.Status;
            
            var orderDomain = LabOrderDomain.Create(order);
            orderDomain.Cancel(command.CancellationReason, cancelledByEmployee);

            await _labOrdersService.UpdateAsync(order);

            await _mediator.Publish(new OrderStatusChangedEvent(order), cancellationToken);

            await _paymentIssueManager.CancelOrderPaymentIssueIfExists(command.Id);

            // If order is already paid but the order is not complete, then refund the order
            if (orderDomain.IsPaid() && originalStatus != OrderStatus.Completed)
            {
                _logger.LogInformation($"Refunding Lab order with id: {command.Id} has been started.");

                try
                {
                    var refund = await _paymentService.RefundOrderPaymentAsync(order.PatientId, order);

                    _logger.LogInformation($"Refunding Lab order with id: {command.Id} refundId: {refund.Id} has been finished.");
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Refunding Lab order with id: {command.Id} has failed with [Error] {e.ToString()}.");
                    throw new AppException(HttpStatusCode.BadRequest, "The order was canceled but unable to refund.  Please refund manually.");
                }
            }
            
            _logger.LogInformation($"Cancelling Lab order with id: {command.Id} has been finished.");

            return order;
        }
    }
}
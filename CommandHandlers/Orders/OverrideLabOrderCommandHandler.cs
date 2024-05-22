using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Models.Orders;
using MediatR;
using WildHealth.Application.Domain.PaymentIssues;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class OverrideLabOrderCommandHandler : IRequestHandler<OverrideLabOrderCommand, LabOrder>
    {
        private readonly ILabOrdersService _labOrdersService;
        private readonly ILogger _logger;
        private readonly IEmployeeService _employeeService;
        private readonly IPaymentIssueManager _paymentIssueManager;

        public OverrideLabOrderCommandHandler(
            ILabOrdersService labOrdersService, 
            ILogger<OverrideLabOrderCommandHandler> logger,
            IEmployeeService employeeService, 
            IPaymentIssueManager paymentIssueManager)
        {
            _labOrdersService = labOrdersService;
            _logger = logger;
            _employeeService = employeeService;
            _paymentIssueManager = paymentIssueManager;
        }

        public async Task<LabOrder> Handle(OverrideLabOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Overriding Lab order with id: {command.Id} has been started.");
                        
            var order = await _labOrdersService.GetByIdAsync(command.Id);
            var overrideByEmployee = await _employeeService.GetByIdAsync(command.OverrideById);

            var orderDomain = LabOrderDomain.Create(order);
            orderDomain.Override(command.OverrideReason, overrideByEmployee);

            await _labOrdersService.UpdateAsync(order);
            
            await _paymentIssueManager.CancelOrderPaymentIssueIfExists(order.GetId());

            return order;
        }
    }
}
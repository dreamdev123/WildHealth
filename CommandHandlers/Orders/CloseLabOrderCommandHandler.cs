using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Models.Orders;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class CloseLabOrderCommandHandler : IRequestHandler<CloseLabOrderCommand, LabOrder>
    {
        private readonly ILabOrdersService _labOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CloseLabOrderCommandHandler(
            ILabOrdersService labOrdersService, 
            IMediator mediator, 
            ILogger<CloseLabOrderCommandHandler> logger)
        {
            _labOrdersService = labOrdersService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<LabOrder> Handle(CloseLabOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Closing of Lab order with id: {command.Id} has been started.");
            
            var date = DateTime.UtcNow;
            
            var order = await _labOrdersService.GetByIdAsync(command.Id);
            
            var labOrderDomain = LabOrderDomain.Create(order);

            labOrderDomain.Close(date);

            await _labOrdersService.UpdateAsync(order);

            await _mediator.Publish(new OrderStatusChangedEvent(order), cancellationToken);
            
            _logger.LogInformation($"Closing of Lab order with id: {command.Id} has been finished.");

            return order;
        }
    }
}
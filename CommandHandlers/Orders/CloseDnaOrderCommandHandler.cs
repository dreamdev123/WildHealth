using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Domain.Entities.Orders;
using Microsoft.Extensions.Logging;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class CloseDnaOrderCommandHandler : IRequestHandler<CloseDnaOrderCommand, DnaOrder>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CloseDnaOrderCommandHandler(
            IDnaOrdersService dnaOrdersService, 
            IMediator mediator,
            ILogger<CloseDnaOrderCommandHandler> logger)
        {
            _dnaOrdersService = dnaOrdersService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<DnaOrder> Handle(CloseDnaOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Closing of DNA order with id: {command.Id} has been started.");
            
            var date = DateTime.UtcNow;
            
            var order = await _dnaOrdersService.GetByIdAsync(command.Id);
            
            order.Close(command.IsSucceeded, date);

            await _dnaOrdersService.UpdateAsync(order);

            await _mediator.Publish(new OrderStatusChangedEvent(order), cancellationToken);
            
            _logger.LogInformation($"Closing of DNA order with id: {command.Id} has been finished.");

            return order;
        }
    }
}
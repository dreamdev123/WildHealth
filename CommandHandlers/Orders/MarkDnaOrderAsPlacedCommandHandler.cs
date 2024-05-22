using System.Linq;
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
    public class MarkDnaOrderAsPlacedCommandHandler : IRequestHandler<MarkDnaOrderAsPlacedCommand, DnaOrder>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public MarkDnaOrderAsPlacedCommandHandler(
            IDnaOrdersService dnaOrdersService,
            IMediator mediator, 
            ILogger<MarkDnaOrderAsPlacedCommandHandler> logger)
        {
            _dnaOrdersService = dnaOrdersService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<DnaOrder> Handle(MarkDnaOrderAsPlacedCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Marking placement of DNA order with id: {command.Id} has been started.");
            
            var order = await _dnaOrdersService.GetByIdAsync(command.Id);

            var items = CreateOrderItems(command);

            order.MarkAsPlaced(
                number: command.Number,
                date: command.Date,
                items: items);

            await _dnaOrdersService.UpdateAsync(order);

            await _mediator.Publish(new OrderStatusChangedEvent(order), cancellationToken);
            
            _logger.LogInformation($"Marking placement of DNA order with id: {command.Id} has been finished.");

            return order;
        }
        
        #region private

        /// <summary>
        /// Creates and returns order items based on command
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private (int id, string sku, string description, decimal price, int quantity)[] CreateOrderItems(MarkDnaOrderAsPlacedCommand command)
        {
            return command.Items.Select(x => (
                    x.Id,
                    x.Sku,
                    x.Description,
                    x.Price,
                    x.Quantity
                )
            ).ToArray();
        }
        
        #endregion
    }
}
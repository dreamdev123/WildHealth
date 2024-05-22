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
    public class ShipDnaOrderCommandHandler : IRequestHandler<ShipDnaOrderCommand, DnaOrder>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public ShipDnaOrderCommandHandler(
            IDnaOrdersService dnaOrdersService,
            IMediator mediator,
            ILogger<ShipDnaOrderCommandHandler> logger)
        {
            _dnaOrdersService = dnaOrdersService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<DnaOrder> Handle(ShipDnaOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Shipping of DNA order with id: {command.Id} has been started.");
            
            var order = await _dnaOrdersService.GetByIdAsync(command.Id);

            order.MarkAsShipped(
                barcode: command.Barcode,
                patientShippingNumber: command.PatientShippingNumber,
                laboratoryShippingNumber: command.LaboratoryShippingNumber,
                date: command.Date
            );

            await _dnaOrdersService.UpdateAsync(order);

            await _mediator.Publish(new OrderStatusChangedEvent(order), cancellationToken);
            
            _logger.LogInformation($"Shipping of DNA order with id: {command.Id} has been finished.");

            return order;
        }
    }
}
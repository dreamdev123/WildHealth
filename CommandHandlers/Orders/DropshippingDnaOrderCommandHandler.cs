using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Domain.Entities.Orders;
using Microsoft.Extensions.Logging;
using MediatR;
using WildHealth.Common.Models.Orders;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;


namespace WildHealth.Application.CommandHandlers.Orders
{
    public class DropshippingDnaOrderCommandHandler : IRequestHandler<DropshippingDnaOrderCommand, DnaOrder>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public DropshippingDnaOrderCommandHandler(
            IDnaOrdersService dnaOrdersService,
            IFeatureFlagsService featureFlagsService,
            IMediator mediator,
            ILogger<DropshippingDnaOrderCommandHandler> logger)
        {
            _dnaOrdersService = dnaOrdersService;
            _featureFlagsService = featureFlagsService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<DnaOrder> Handle(DropshippingDnaOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Updating DNA order with id: {command.Id} has been started.");
            
            var order = await _dnaOrdersService.GetByIdAsync(command.Id);

            order.UpdateDnaOrderInformation(
                number: command.Number,
                barcode: command.Barcode,
                patientShippingNumber: command.PatientShippingNumber,
                laboratoryShippingNumber: command.LaboratoryShippingNumber
            );

            await _dnaOrdersService.UpdateAsync(order);

            var items = CreateOrderItems(order.Items);

            await _mediator.Send(
                new MarkDnaOrderAsPlacedCommand(
                    order.GetId(), 
                    command.Number, 
                    DateTime.UtcNow, 
                    items), 
                cancellationToken);
            
            _logger.LogInformation($"Updating DNA order with id: {command.Id} has been finished.");
            
            return order;
        }
        
        #region private
        
        /// <summary>
        /// Creates and returns order items based on command
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private PlaceOrderItemModel[] CreateOrderItems(ICollection<OrderItem> items)
        {
            return items.Select(x => new PlaceOrderItemModel()
                {
                    Id = x.GetId(),
                    Description = x.Description,
                    Price = x.Price,
                    Quantity = x.Quantity,
                    Sku = x.Sku
                }
            ).ToArray();
        }
        
        #endregion
    }
}
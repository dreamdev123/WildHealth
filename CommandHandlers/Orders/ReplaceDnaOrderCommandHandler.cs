using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Interfaces;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class ReplaceDnaOrderCommandHandler : IRequestHandler<ReplaceDnaOrderCommand, (DnaOrder replacedOrder, DnaOrder newOrder)>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public ReplaceDnaOrderCommandHandler(
            IDnaOrdersService dnaOrdersService, 
            IPermissionsGuard permissionsGuard,
            IMediator mediator,
            ILogger<ReplaceDnaOrderCommandHandler> logger)
        {
            _dnaOrdersService = dnaOrdersService;
            _permissionsGuard = permissionsGuard;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<(DnaOrder replacedOrder, DnaOrder newOrder)> Handle(ReplaceDnaOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Replacement of DNA order with id: {command.Id} has been started.");
            
            var date = DateTime.UtcNow;
            
            var order = await _dnaOrdersService.GetByIdAsync(command.Id);
            
            _permissionsGuard.AssertPermissions((IPatientRelated) order);

            await AssertOrderCanBeReplaced(order);

            var items = CreateOrderItems(order.Items);
            
            var newOrder = new DnaOrder(
                patient: order.Patient,
                replacementOrder: order,
                replacementReason: command.Reason,
                items: items,
                date: date
            );

            await _dnaOrdersService.CreateAsync(newOrder);

            await _mediator.Publish(new OrderCreatedEvent(newOrder), cancellationToken);
            
            _logger.LogInformation($"Replacement of DNA order with id: {command.Id} has been finished.");
            
            return (order, newOrder);
        }
        
        #region private

        /// <summary>
        /// Asserts order can be replaced, otherwise throw an exception
        /// </summary>
        /// <param name="order"></param>
        /// <exception cref="AppException"></exception>
        private async Task AssertOrderCanBeReplaced(DnaOrder order)
        {
            var isReplaced = await _dnaOrdersService.IsReplacedAsync(order.GetId());
                
            if (isReplaced)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Order can't be replaced.");
            }
            
            if (!order.CanReplace())
            {
                throw new AppException(HttpStatusCode.BadRequest, "Order can't be replaced.");
            }
        }
        
        /// <summary>
        /// Creates and returns order items based on replaced order items
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private OrderItem[] CreateOrderItems(IEnumerable<OrderItem> items)
        {
            return items.Select(item =>
            {
                var newItem = new OrderItem(item.AddOn);

                newItem.FillOut(
                    sku: item.AddOn.IntegrationId,
                    description: item.AddOn.Name,
                    price: item.AddOn.GetPrice(),
                    quantity: 1
                );

                return newItem;
            }).ToArray();
        }
        
        #endregion
    }
}
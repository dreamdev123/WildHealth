using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Models.Orders;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class FinalizeLabOrderCommandHandler : IRequestHandler<FinalizeLabOrderCommand>
    {
        private readonly ILabOrdersService _labOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public FinalizeLabOrderCommandHandler(
            ILabOrdersService labOrdersService, 
            IMediator mediator,
            ILogger<FinalizeLabOrderCommandHandler> logger)
        {
            _labOrdersService = labOrdersService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(FinalizeLabOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Processing payment on lab order {command.OrderNumber} completed event has been started.");
            
            var order = await _labOrdersService.GetByNumberAsync(
                number: command.OrderNumber, 
                patientId: command.PatientId
            );
            var orderDomain = LabOrderDomain.Create(order);
                
            if (orderDomain.ShouldBill())
            {
                // Order can be paid manually or cancelled before completion
                // Please check it before triggering flow to avoid exceptions
                await TryPayForLabOrderAsync(order, cancellationToken);
            }

            if (order.Status != OrderStatus.Completed)
            {
                // Order can be already closed
                // Please check it before triggering flow to avoid exceptions
                await _mediator.Send(new CloseLabOrderCommand(order.GetId()), cancellationToken);
            }
                
            _logger.LogInformation($"Processing payment on lab order {command.OrderNumber} completed event has been finished.");
        }

        /// <summary>
        /// Trying to proceed lab order payment
        /// </summary>
        /// <remarks>
        /// https://wildhealth.atlassian.net/browse/CLAR-1580?atlOrigin=eyJpIjoiODY3ZjM2MjQ5ZGE4NGJlYmI4N2Y3MzNjYWM2NzkxNWYiLCJwIjoiaiJ9
        /// Requested that we bill for labs at checkout.  Given the integration with Change Healthcare, we will bill when we receive
        /// the ReceiveLabOrderEvent
        /// If payment error occured - logs the error and continue the flow
        /// </remarks>
        /// <param name="order"></param>
        /// <param name="cancellationToken"></param>
        private async Task TryPayForLabOrderAsync(LabOrder order, CancellationToken cancellationToken)
        {
            var orderDomain = OrderDomain.Create(order);
            if (orderDomain.IsPaid())
            {
                return;
            }
            
            try
            {
                await _mediator.Send(new PayLabOrderCommand(
                    id: order.GetId(),
                    shouldSendOrderInvoiceEmail: true
                ), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during paying for lab order with id: {order.GetId()} - {e.Message} with [Error]: {e.ToString()}");
            }
        }
    }
}




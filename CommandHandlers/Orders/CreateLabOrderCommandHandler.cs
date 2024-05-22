using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Domain.Entities.Orders;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Application.CommandHandlers.Orders.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Models.Timeline;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class CreateLabOrderCommandHandler : IRequestHandler<CreateLabOrderCommand, LabOrder>
    {
        private readonly IPatientsService _patientsService;
        private readonly IAddOnsService _addOnsService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IFlowMaterialization _materializer;

        public CreateLabOrderCommandHandler(
            IPatientsService patientsService,
            IAddOnsService addOnsService, 
            IMediator mediator,
            ILogger<CreateLabOrderCommandHandler> logger, 
            IFlowMaterialization materializer)
        {
            _patientsService = patientsService;
            _addOnsService = addOnsService;
            _mediator = mediator;
            _logger = logger;
            _materializer = materializer;
        }
        
        public async Task<LabOrder> Handle(CreateLabOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Creating Lab order for patient with id: {command.PatientId} has been started.");
            
            var date = DateTime.UtcNow;

            var number = command.OrderNumber;
            
            var patient = await _patientsService.GetByIdAsync(command.PatientId);
            
            var addOns = await FetchAddOnsAsync(command.AddOnIds, patient.User.PracticeId);

            AssertAddOnsType(addOns);

            var orderItems = CreateOrderItems(addOns);

            var order = await new CreateLabOrderFlow(patient, number, orderItems, addOns.First().Provider, date)
                .Materialize(_materializer.Materialize)
                .Select<LabOrder>();

            await _materializer.Materialize(new LabsOrderedTimelineEvent(
                order.PatientId,
                date,
                new LabsOrderedTimelineEvent.Data(order.GetId())).Added());
            
            await _mediator.Publish(new OrderCreatedEvent(order), cancellationToken);

            // It's important to know when lab order were created, it determines whether we try to apply coupon code credits or even bill for labs
            // If they were ordered at checkout, we want to try and apply credits if a coupon code was applied
            // Additionally, if they were ordered at checkout, we want to try and charge for the labs
            // Labs ordered after initial checkout should NOT be billed for when placed and instead billed for when results are received
            // https://wildhealth.atlassian.net/browse/CLAR-1580?atlOrigin=eyJpIjoiODY3ZjM2MjQ5ZGE4NGJlYmI4N2Y3MzNjYWM2NzkxNWYiLCJwIjoiaiJ9
            var isOrderCreatedAtCheckout = await _mediator.Send(new OrderCreatedAtCheckoutCommand(
                order: order
            ));

            // Payment right now is dependent on whether labs were created at checkout or not
            if (isOrderCreatedAtCheckout)
            {
                await _mediator.Send(new PayLabOrderCommand(
                    id: order.GetId(),
                    shouldSendOrderInvoiceEmail: false
                ), cancellationToken);
            }
            
            _logger.LogInformation($"Creating Lab order for patient with id: {command.PatientId} has been finished.");

            return order;
        }
        
        #region private

        /// <summary>
        /// Asserts if add-on types matches with order type
        /// </summary>
        /// <param name="addOns"></param>
        /// <exception cref="AppException"></exception>
        private void AssertAddOnsType(AddOn[] addOns)
        {
            if (addOns.Any(x => x.OrderType != OrderType.Lab))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Add on type and order type does not match.");
            }
        }
        
        /// <summary>
        /// Fetches and returns add-ons by ids
        /// </summary>
        /// <param name="addOnIds"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        private async Task<AddOn[]> FetchAddOnsAsync(int[] addOnIds, int practiceId)
        {
            var addOns = await _addOnsService.GetByIdsAsync(addOnIds, practiceId);

            return addOns.ToArray();
        }
        
        /// <summary>
        /// Creates and returns order items based on add-ons
        /// </summary>
        /// <param name="addOns"></param>
        /// <returns></returns>
        private OrderItem[] CreateOrderItems(AddOn[] addOns)
        {
            return addOns.Select(addOn =>
            {
                var item = new OrderItem(addOn);

                item.FillOut(
                    sku: addOn.IntegrationId,
                    description: addOn.Name,
                    price: addOn.GetPrice(),
                    quantity: 1
                );

                return item;
            }).ToArray();
        }
        
        #endregion
    }
}
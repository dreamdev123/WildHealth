using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.Inputs;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Application.Commands.Inputs;
using MediatR;

namespace WildHealth.Application.EventHandlers.Inputs
{
    public class TryFinalizeOrderOnFileInputUploaded :  INotificationHandler<FileInputsUploadedEvent>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly ILabOrdersService _labOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        
        public TryFinalizeOrderOnFileInputUploaded(
            IDnaOrdersService dnaOrdersService,
            ILabOrdersService labOrdersService,
            IMediator mediator,
            ILogger<TryCloseDnaOrderOnFileInputUploaded> logger)
        {
            _dnaOrdersService = dnaOrdersService;
            _labOrdersService = labOrdersService;
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task Handle(FileInputsUploadedEvent @event, CancellationToken cancellationToken)
        {
            if (@event.InputType != FileInputType.LabResults)
            {
                return;
            }
            
            _logger.LogInformation($"Locating lab orders that are NOT closed for [PatientId]: {@event.PatientId} has started.");

            LabOrder? pendingOrder = null;

            var orders = await _labOrdersService.GetPatientOrdersAsync(@event.PatientId);

            // If we have a specific order number then we want to use that here
            if(!string.IsNullOrEmpty(@event.OrderNumber)) {
                pendingOrder = orders.FirstOrDefault(x => x.Number == @event.OrderNumber);
            } else {
                pendingOrder = orders.FirstOrDefault(x => x.Status != OrderStatus.Completed);
            }

            // If the resulting order has already been cancelled, then the order 
            // was likely replaced and we need to find the order the results should be associated with
            // See: https://wildhealth.atlassian.net/browse/CLAR-5288
            if(pendingOrder is not null && pendingOrder.Status == OrderStatus.Cancelled)
            {
                // IsReplacementOrder will try to find an active order that has an ExpectedCollectionDate within
                // 30 days before or after the resulting order
                pendingOrder = orders.FirstOrDefault(x => IsReplacementOrder(x, pendingOrder));
            }

            if (pendingOrder is null)
            {
                _logger.LogInformation($"Locating lab orders that are NOT closed for [PatientId]: {@event.PatientId} resulted in no orders being found.");
                
                return;
            }

            _logger.LogInformation($"Located lab order with number [Number]: {pendingOrder.Number}.");

            _logger.LogInformation($"Initiating finalization of lab order with [Number]: {pendingOrder.Number}");

            await _mediator.Send(new FinalizeLabOrderCommand(
                patientId: @event.PatientId,
                orderNumber: pendingOrder.Number
            ), cancellationToken);
        }

        private bool IsReplacementOrder(Order newOrder, Order cancelledOrder)
        {
            if(newOrder.ExpectedCollectionDate is null || cancelledOrder.ExpectedCollectionDate is null)
            {
                return false;
            }

            // If the order we are looking for is cancelled, it is possible there was a replacement order created due to a mistake
            // Find an order that has not been cancelled, and has a collection date withing 30 days of the cancelled
            // order and use that as the replacement.
            var timeDiff = ((DateTime)newOrder.ExpectedCollectionDate).Subtract((DateTime)cancelledOrder.ExpectedCollectionDate);

            if(Math.Abs(timeDiff.TotalDays) <= 30)
            {
                return true;
            }
            return false;
        }
    }
}
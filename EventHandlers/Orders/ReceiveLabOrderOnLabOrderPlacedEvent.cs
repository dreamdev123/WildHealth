using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Orders;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using MediatR;

namespace WildHealth.Application.EventHandlers.Orders
{
    public class ReceiveLabOrderOnLabOrderPlacedEvent : INotificationHandler<LabOrderPlacedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public ReceiveLabOrderOnLabOrderPlacedEvent(
            IMediator mediator, 
            ILogger<ReceiveLabOrderOnLabOrderPlacedEvent> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(LabOrderPlacedEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Receiving lab order on lab order {@event.OrderNumber} placed event has been started.");

            var command = new ReceiveLabOrderCommand(
                patientId: @event.PatientId,
                reportId: @event.ReportId,
                orderNumber: @event.OrderNumber,
                testCodes: @event.TestCodes
            );
            
            await _mediator.Send(command, cancellationToken);
                
            _logger.LogInformation($"Receiving lab order on lab order {@event.OrderNumber} placed event has been finished.");
        }
    }
}
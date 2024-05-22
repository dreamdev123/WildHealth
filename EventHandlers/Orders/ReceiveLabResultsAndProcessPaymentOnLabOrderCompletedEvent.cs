using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.Orders.Lab;
using MediatR;

namespace WildHealth.Application.EventHandlers.Orders
{
    public class ReceiveLabResultsOnLabOrderCompletedEvent : INotificationHandler<LabOrderCompletedEvent>
    {
        private readonly ILabOrdersService _labOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public ReceiveLabResultsOnLabOrderCompletedEvent(
            ILabOrdersService labOrdersService,
            IMediator mediator, 
            ILogger<ReceiveLabResultsOnLabOrderCompletedEvent> logger)
        {
            _labOrdersService = labOrdersService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(LabOrderCompletedEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Receiving lab results on lab order {@event.OrderNumber} completed event has been started.");

            var command = new ReceiveLabResultsCommand(
                patientId: @event.PatientId,
                reportId: @event.ReportId
            );
            
            await _mediator.Send(command, cancellationToken);
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Orders;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using MediatR;

namespace WildHealth.Application.EventHandlers.Orders
{
    public class CorrectLabResultsOnLabOrderCorrectedEvent : INotificationHandler<LabOrderCorrectedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CorrectLabResultsOnLabOrderCorrectedEvent(
            IMediator mediator, 
            ILogger<CorrectLabResultsOnLabOrderCorrectedEvent> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(LabOrderCorrectedEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Correcting lab results on lab order {@event.OrderNumber} corrected event has been started.");

            var command = new CorrectLabResultsCommand(
                patientId: @event.PatientId,
                reportId: @event.ReportId,
                orderNumber: @event.OrderNumber
            );
            
            await _mediator.Send(command, cancellationToken);
                
            _logger.LogInformation($"Correcting lab results on lab order {@event.OrderNumber} corrected event has been finished.");
        }
    }
}
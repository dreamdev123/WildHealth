using System.Threading;
using System.Threading.Tasks;
using Automatonymous;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Events.Reports;


namespace WildHealth.Application.EventHandlers.Reports
{

    public class HealthSummaryApoeUpdateOnHealthReportUpdatedEvent : INotificationHandler<HealthReportUpdatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<HealthSummaryApoeUpdateOnHealthReportUpdatedEvent> _logger;

        public HealthSummaryApoeUpdateOnHealthReportUpdatedEvent(IMediator mediator,
            ILogger<HealthSummaryApoeUpdateOnHealthReportUpdatedEvent> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task Handle(HealthReportUpdatedEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Calling update APOE Data on Health Summary for Patient [id]:{@event.PatientId}");
            await _mediator.Send(new UpdatePatientApoeHealthSummaryCommand(@event.PatientId,
                @event.Report), cancellationToken);
        }
    }
}
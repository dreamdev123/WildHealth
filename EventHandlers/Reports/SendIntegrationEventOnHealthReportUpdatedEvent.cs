using System;
using System.Threading;
using System.Threading.Tasks;
using Automatonymous;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Events.Reports;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Reports;
using WildHealth.IntegrationEvents.Reports.Payloads;


namespace WildHealth.Application.EventHandlers.Reports
{

    public class SendIntegrationEventOnHealthReportUpdatedEvent : INotificationHandler<HealthReportUpdatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SendIntegrationEventOnHealthReportUpdatedEvent> _logger;
        private readonly IEventBus _eventBus;
        private readonly IPatientsService _patientsService;

        public SendIntegrationEventOnHealthReportUpdatedEvent(IMediator mediator,
            ILogger<SendIntegrationEventOnHealthReportUpdatedEvent> logger,
            IEventBus eventBus,
            IPatientsService patientsService
            )
        {
            _mediator = mediator;
            _logger = logger;
            _eventBus = eventBus;
            _patientsService = patientsService;
        }
        
        public async Task Handle(HealthReportUpdatedEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Sending integration event Patient [id]:{@event.PatientId}");

            var patient = await _patientsService.GetByIdAsync(@event.PatientId);
            
            await _eventBus.Publish(new ReportIntegrationEvent(
                payload: new ReportCreatedPayload(
                    reportTypeId: Convert.ToInt16(ReportType.Health),
                    reportTypeName: ReportType.Health.ToString()
                ),
                patient: new PatientMetadataModel(id: patient.GetId(), universalId: patient.User.UserId()),
                eventDate: DateTime.UtcNow), cancellationToken);
        }
    }
}
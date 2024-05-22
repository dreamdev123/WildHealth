using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Reports;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Reports.Template;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Reports;
using WildHealth.IntegrationEvents.Reports.Payloads;

namespace WildHealth.Application.EventHandlers.Reports;

public class SendEventForAvailableReportsOnAddonReportMetricsCreatedEvent : INotificationHandler<PatientAddOnReportMetricsCreatedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly IReportTemplateService _reportTemplateService;
    private readonly IPatientsService _patientsService;

    public SendEventForAvailableReportsOnAddonReportMetricsCreatedEvent(
        IEventBus eventBus,
        IReportTemplateService reportTemplateService,
        IPatientsService patientsService
    )
    {
        _eventBus = eventBus;
        _reportTemplateService = reportTemplateService;
        _patientsService = patientsService;
    }

    public async Task Handle(PatientAddOnReportMetricsCreatedEvent @event, CancellationToken cancellationToken)
    {
        var reportTemplates = await _reportTemplateService.GetByStrategiesAsync(new ReportStrategy[] { ReportStrategy.AddOnFree, ReportStrategy.AddOnPaid});
        var patientUniversalId = await _patientsService.GetUniversalIdForPatientId(@event.PatientId);
        foreach (var reportTemplate in reportTemplates)
        {
            var reportTemplateMetrics = reportTemplate.ReportTemplateMetrics;
            var populatedMetrics = @event.PatientMetrics.Select(x => x.Metric);
            
            if (reportTemplateMetrics.Any(x => !populatedMetrics.Contains(x.Metric)))
            {
                // Means there was a ReportTemplateMetric for the ReportTemplate where there was not a corresponding new PatientMetric
                // Implies that the report is not available to generate so we do not want to send an event
                continue;
            }

            await _eventBus.Publish(new ReportIntegrationEvent(
                payload: new AddOnReportGeneratedPayload(
                    reportTypeId: (int)reportTemplate.ReportType,
                    reportTypeName: reportTemplate.ReportType.ToString()
                ),
                patient: new PatientMetadataModel(@event.PatientId, patientUniversalId),
                eventDate: DateTime.UtcNow
            ));
        }
    }
}
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Metrics;
using WildHealth.Application.Events.PatientRecommendations;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.EventHandlers.PatientRecommendations;

public class CreatePatientMetricsOnMetricSourceUpdatedEvent : INotificationHandler<PatientMetricSourcesUpdatedEvent>
{
    private readonly IMediator _mediator;

    public CreatePatientMetricsOnMetricSourceUpdatedEvent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(PatientMetricSourcesUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var patientId = notification.PatientId;
        var metricSources = notification.MetricSources;
        
        var command = new CreatePatientMetricsBySourceCommand(patientId, metricSources);

        await _mediator.Send(command, cancellationToken);
    }
}
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Recommendations;
using WildHealth.Application.Events.PatientRecommendations;

namespace WildHealth.Application.EventHandlers.PatientRecommendations;

public class UpdatePatientRecommendationsOnMetricsUpdatedEvent : INotificationHandler<PatientMetricsUpdatedEvent>
{
    private readonly IMediator _mediator;

    public UpdatePatientRecommendationsOnMetricsUpdatedEvent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(PatientMetricsUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var command = new UpdatePatientRecommendationsCommand(notification.PatientId, notification.MetricSources);

        await _mediator.Send(command, cancellationToken);
    }
}
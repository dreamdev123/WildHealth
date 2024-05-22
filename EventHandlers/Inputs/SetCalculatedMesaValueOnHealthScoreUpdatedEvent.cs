using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Events.Reports;

namespace WildHealth.Application.EventHandlers.Inputs;

public class SetCalculatedMesaValueOnHealthScoreUpdatedEvent : INotificationHandler<HealthScoreUpdatedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SetCalculatedMesaValueOnHealthScoreUpdatedEvent> _logger;

    public SetCalculatedMesaValueOnHealthScoreUpdatedEvent(
        IMediator mediator,
        ILogger<SetCalculatedMesaValueOnHealthScoreUpdatedEvent> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(HealthScoreUpdatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            $"Calling handler to set MESA value on General input for Patient [id]:{@event.PatientId}");

        var command = new SetPatientMesaValueOnInputCommand(
            patientId: @event.PatientId
        );
        await _mediator.Send(command, cancellationToken);

    }

}
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Metrics;
using WildHealth.Application.Events.Inputs;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.EventHandlers.Inputs;

public class CreatePatientMetricsOnInputsUpdatedEvent : INotificationHandler<LabInputsUpdatedEvent>, INotificationHandler<MicrobiomeInputsUpdatedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CreatePatientMetricsOnInputsUpdatedEvent> _logger;
    
    public CreatePatientMetricsOnInputsUpdatedEvent(
        IMediator mediator,
        ILogger<CreatePatientMetricsOnInputsUpdatedEvent> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(LabInputsUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await CreatePatientMetrics(notification.PatientId, new[] { MetricSource.Labs });
    }
    
    public async Task Handle(MicrobiomeInputsUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await CreatePatientMetrics(notification.PatientId, new[] { MetricSource.Microbiome });
    }

    private async Task CreatePatientMetrics(int patientId, MetricSource[] sources)
    {
        try
        {
            var command = new CreatePatientMetricsBySourceCommand(
                patientId: patientId, 
                sources: sources);

            await _mediator.Send(command);
        }
        catch (Exception e)
        {
            _logger.LogWarning($"Creating of patient metrics on lab inputs updated for [PatientId] = {patientId} has failed with [Error]: {e}");
        }
    }
}
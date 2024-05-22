using System.Linq;
using MediatR;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.Events.PatientRecommendations;

public record PatientMetricsUpdatedEvent(
    int PatientId,
    int[] MetricIds,
    MetricSource[] MetricSources) : INotification
{
    public PatientMetricsUpdatedEvent(
        int patientId,
        int[] metricIds,
        int[] metricSources) : this(patientId, metricIds, metricSources.Cast<MetricSource>().ToArray())
    {
        
    }
}
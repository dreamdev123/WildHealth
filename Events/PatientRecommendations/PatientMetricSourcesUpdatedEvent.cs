using System.Linq;
using MediatR;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.Events.PatientRecommendations;

public record PatientMetricSourcesUpdatedEvent(int PatientId, MetricSource[] MetricSources) : INotification
{
    public PatientMetricSourcesUpdatedEvent(
        int patientId,
        int[] metricSources) : this(patientId, metricSources.Cast<MetricSource>().ToArray())
    {
        
    }
}
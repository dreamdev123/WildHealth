using WildHealth.Application.Events.PatientRecommendations;
using WildHealth.IntegrationEvents.Recommendations.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;

public static class PatientRecommendationsIntegrationEventExtensions
{
    public static PatientMetricsUpdatedEvent ToPatientMetricsUpdatedEvent(this PatientMetricsUpdatedPayload source) =>
       new(source.PatientId, source.MetricIds, source.MetricSources);

    public static PatientMetricSourcesUpdatedEvent ToPatientMetricSourcesUpdatedEvent(this PatientMetricSourcesUpdatedPayload source) =>
        new(source.PatientId, source.MetricSources);
}
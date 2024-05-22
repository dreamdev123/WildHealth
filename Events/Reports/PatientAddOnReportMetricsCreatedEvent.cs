using MediatR;
using WildHealth.Domain.Entities.Metrics;

namespace WildHealth.Application.Events.Reports;

public class PatientAddOnReportMetricsCreatedEvent : INotification
{
    public PatientAddOnReportMetricsCreatedEvent(int patientId, PatientMetric[] patientMetrics)
    {
        PatientId = patientId;
        PatientMetrics = patientMetrics;
    }
    public int PatientId { get; set; }
    public PatientMetric[] PatientMetrics { get; set; }
}
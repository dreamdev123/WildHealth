using MediatR;
using WildHealth.Domain.Entities.Reports;

namespace WildHealth.Application.Events.Reports
{
    public class HealthReportUpdatedEvent : INotification
    {
        public int PatientId { get; }
        
        public HealthReport Report { get; }

        public HealthReportUpdatedEvent(int patientId, HealthReport report)
        {
            PatientId = patientId;
            Report = report;
        }

    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Reports;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Reports;

namespace WildHealth.Application.Services.Reports
{
    public interface IPatientReportService
    {
        Task<PatientReport> GetLatestAsync(ReportType reportType, int patientId);
        Task<PatientReport> GetByTypeAndIdAsync(ReportType reportType, int patientReportId);
        Task<PatientReport> CreateReportAsync(PatientReport patientReport);
        Task<(bool, string)> TryPrepareForGeneration(int patientId, ReportTemplate reportTemplate);
        PatientReport GenerateReport(int patientId, ReportTemplate reportTemplate, List<PatientMetric> reportPatientMetrics, List<PatientRecommendation> patientRecommendations);
        Task<List<PatientAddOnReportModel>> GetAvailableReports(ReportStrategy reportStrategy, int patientId);
    }
}
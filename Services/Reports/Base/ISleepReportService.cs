using System.Threading.Tasks;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Domain.Enums.Reports;

namespace WildHealth.Application.Services.Reports.Base;

public interface ISleepReportService
{
    Task<PatientReport> GetLatestAsync(int patientId, ReportType reportType);
    Task<PatientReport> CreateAsync(int patientId, ReportType reportType, HealthCategoryTag recommendationsTag);
}
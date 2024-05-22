using System.Threading.Tasks;
using WildHealth.Domain.Entities.Reports._Base;
using WildHealth.Domain.Enums.Reports;

namespace WildHealth.Application.Services.HealthReports.Helpers
{
    public interface IReportService
    {
        public Task<ReportEntityBase> GetLatestAsync(ReportType reportType, int patientId);
        public Task<ReportEntityBase> CreateAsync(ReportType reportType, int patientId);
        public Task<ReportEntityBase> GetLatestOrCreateAsync(ReportType reportType, int patientId);
    }
}
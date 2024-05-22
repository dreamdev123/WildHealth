using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Reports.Base;
using WildHealth.Application.Services.Reports.Celiac;
using WildHealth.Application.Utils.Reports;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Domain.Entities.Reports._Base;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.HealthReports.Helpers
{
    public class ReportService : IReportService
    {
        private readonly ReportServiceRegistrationHelper.ServiceResolver _serviceResolver;
        private readonly IHealthReportService _healthReportService;
        private readonly ICeliacReportService _celiacReportService;
        private readonly ISleepReportService _sleepReportService;
        private readonly ILogger<IReportService> _logger;

        public ReportService(
            ReportServiceRegistrationHelper.ServiceResolver serviceResolver,
            ILogger<ReportService> logger, 
            ISleepReportService sleepReportService)
        {
            _serviceResolver = serviceResolver;
            _healthReportService = (IHealthReportService) _serviceResolver(ReportType.Health);
            _celiacReportService = (ICeliacReportService) _serviceResolver(ReportType.Celiac);
            _logger = logger;
            _sleepReportService = sleepReportService;
        }

        public async Task<ReportEntityBase> GetLatestOrCreateAsync(ReportType reportType, int patientId)
        {
            return reportType switch
            {
                ReportType.Health => await _healthReportService.GetLatestOrCreateAsync(patientId),
                _ => await GetLatestAsyncSafe(reportType, patientId)
            };    
        }

        public async Task<ReportEntityBase> GetLatestAsync(ReportType reportType, int patientId)
        {
            return reportType switch
            {
                ReportType.Health => await _healthReportService.GetLatestAsync(patientId),
                ReportType.Celiac => await _celiacReportService.GetLatestAsync(patientId),
                ReportType.Sleep => await _sleepReportService.GetLatestAsync(patientId, ReportType.Sleep),
                _ => throw new AppException(HttpStatusCode.NotFound,
                    $"Could not generate report with type {reportType}")
            };
        }

        public async Task<ReportEntityBase> CreateAsync(ReportType reportType, int patientId)
        {
            return reportType switch
            {
                ReportType.Health => await _healthReportService.CreateAsync(patientId),
                ReportType.Celiac => await _celiacReportService.CreateAsync(patientId),
                ReportType.Sleep => await _sleepReportService.CreateAsync(patientId, ReportType.Sleep, HealthCategoryTag.Sleep),
                _ => throw new AppException(HttpStatusCode.NotFound,
                    $"Could not generate report with type {reportType}")
            };
        }

        private async Task<ReportEntityBase> GetLatestAsyncSafe(ReportType reportType, int patientId)
        {
            var result = await GetLatestAsync(reportType, patientId).ToTry();

            if (result.IsError() && result.Exception() is AppException {StatusCode: HttpStatusCode.NotFound})
            {
                _logger.LogInformation($"Unable to find {reportType.ToString()} Report for PatientId {patientId}. Creating new...");
                
                return await CreateAsync(reportType, patientId);
            }

            return result.SuccessValue();
        }
    }
}
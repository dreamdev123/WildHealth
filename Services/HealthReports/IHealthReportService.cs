using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Reports;

namespace WildHealth.Application.Services.HealthReports
{
    /// <summary>
    /// Provides methods for working with health report
    /// </summary>
    public interface IHealthReportService
    {
        /// <summary>
        /// Returns reports reviewing by particular employee
        /// </summary>
        /// <param name="reviewingBy"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        Task<(IEnumerable<HealthReport> reports, int totalCount)> GetReportsReviewingByAsync(
            int reviewingBy,
            int? skip,
            int? take);

        /// <summary>
        /// <see cref="IHealthReportService.GetReportsCompletedByAsync"/>
        /// </summary>
        /// <param name="completedBy"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        Task<(IEnumerable<HealthReport> reports, int totalCount)> GetReportsCompletedByAsync(
            int completedBy,
            int? skip,
            int? take);

        /// <summary>
        /// Returns true if there are reports for review for the specific employee
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        Task<bool> ReportsUnderReviewExist(int employeeId);

        /// <summary>
        /// Returns patient reports
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        Task<IEnumerable<HealthReport>> GetReportsAsync(int patientId, HealthReportStatus? status = null);
        
        /// <summary>
        /// Returns patient latest health report or creates new one
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<HealthReport> GetLatestOrCreateAsync(int patientId);
        
        /// <summary>
        /// Returns patient latest health report 
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        Task<HealthReport> GetLatestAsync(int patientId, HealthReportStatus? status = null);

        /// <summary>
        /// Returns patient health report
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<HealthReport> GetAsync(int id, int patientId);
        
        /// <summary>
        /// Generates health report
        /// </summary>
        /// <param name="report"></param>
        /// <param name="aggregator"></param>
        /// <returns></returns>
        Task<HealthReport> GenerateAsync(HealthReport report, InputsAggregator aggregator);

        /// <summary>
        /// Updates patient health report
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        Task<HealthReport> UpdateAsync(HealthReport report);

        /// <summary>
        /// Creates health report
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<HealthReport> CreateAsync(int patientId);
        
        /// <summary>
        /// Submits health report
        /// </summary>
        /// <param name="report"></param>
        /// <param name="reviewer"></param>
        /// <returns></returns>
        Task<HealthReport> SendToReviewAsync(HealthReport report, Employee reviewer);
        
        /// <summary>
        /// Submits health report
        /// </summary>
        /// <param name="report"></param>
        /// <param name="reviewer"></param>
        /// <returns></returns>
        Task<HealthReport> SubmitAsync(HealthReport report, Employee reviewer);
    }
}
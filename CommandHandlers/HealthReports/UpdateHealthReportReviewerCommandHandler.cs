using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.HealthReports;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.HealthReports;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Employees;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Utils.PermissionsGuard;
using MediatR;

namespace WildHealth.Application.CommandHandlers.HealthReports
{
    public class UpdateHealthReportReviewerCommandHandler : IRequestHandler<UpdateHealthReportReviewerCommand, HealthReport>
    {
        private readonly IEmployeeService _employeeService;
        private readonly IHealthReportService _healthReportService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly ILogger _logger;

        public UpdateHealthReportReviewerCommandHandler(
            IEmployeeService employeeService,
            IHealthReportService healthReportService,
            INotificationService notificationService,
            IPermissionsGuard permissionsGuard,
            ILogger<PublishHealthReportCommandHandler> logger)
        {
            _employeeService = employeeService;
            _healthReportService = healthReportService;
            _notificationService = notificationService;
            _permissionsGuard = permissionsGuard;
            _logger = logger;
        }

        public async Task<HealthReport> Handle(UpdateHealthReportReviewerCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Started updating review for health report with [Id]: {command.Id} to [ReviewerId]: {command.ReviewerId}");

            var report = await GetReportAsync(id: command.Id, patientId: command.PatientId);
            
            _permissionsGuard.AssertPermissions(report);

            var reviewer = await GetReviewerAsync(command.ReviewerId);
            
            _permissionsGuard.AssertPermissions(reviewer);

            report.SetReviewer(reviewer);
                
            await _healthReportService.UpdateAsync(report);

            var notification = new HealthReportReviewNotification(reviewer, report);

            await _notificationService.CreateNotificationAsync(notification);
            
            _logger.LogInformation($"Reviewer for health report with [Id]: {command.Id} successfully changed to {reviewer.GetId()}");

            return report;
        }
        
        #region private

        /// <summary>
        /// Returns health report
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<HealthReport> GetReportAsync(int id, int patientId)
        {
            var report = await _healthReportService.GetAsync(id: id, patientId: patientId);
            
            if (report.IsSubmitted())
            {
                _logger.LogError($"Health report with [Id] = {report.Id} already submitted.");
                
                throw new AppException(HttpStatusCode.BadRequest, $"Health report already submitted.");
            }

            if (!report.IsUnderReview())
            {
                _logger.LogError($"Health report with [Id] = {report.Id} is not under review.");
                
                throw new AppException(HttpStatusCode.BadRequest, $"Send health report to review first.");
            }

            return report;
        }

        /// <summary>
        /// Returns health report reviewer
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<Employee> GetReviewerAsync(int id)
        {
            var reviewer = await _employeeService.GetByIdAsync(id);

            if (reviewer.Type != EmployeeType.Provider)
            {
                _logger.LogError($"Selected reviewer is not a provider.");
                
                throw new AppException(HttpStatusCode.BadRequest, "Selected employee is not a provider.");
            }

            return reviewer;
        }
        
        #endregion
    }
}
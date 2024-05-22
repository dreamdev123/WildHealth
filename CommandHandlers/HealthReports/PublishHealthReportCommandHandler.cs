using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.HealthReports;
using WildHealth.Domain.Entities.Reports;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.HealthReports;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Services.Employees;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Enums.Employees;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Settings;
using Microsoft.Extensions.Options;
using MediatR;

namespace WildHealth.Application.CommandHandlers.HealthReports
{
    public class PublishHealthReportCommandHandler : IRequestHandler<PublishHealthReportCommand, HealthReport>
    {
        private static readonly string[] EmailContainerSettings =
        {
            SettingsNames.General.ApplicationBaseUrl,
            SettingsNames.Emails.HeaderUrl,
            SettingsNames.Emails.LogoUrl
        };

        private readonly IEmployeeService _employeeService;
        private readonly IHealthReportService _healthReportService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly ISettingsManager _settingsManager;
        private readonly AppOptions _options;
        private readonly ILogger _logger;

        public PublishHealthReportCommandHandler(
            IEmployeeService employeeService,
            IHealthReportService healthReportService,
            INotificationService notificationService,
            IPermissionsGuard permissionsGuard,
            ISettingsManager settingsManager,
            IOptions<AppOptions> options,
            ILogger<PublishHealthReportCommandHandler> logger)
        {
            _employeeService = employeeService;
            _healthReportService = healthReportService;
            _notificationService = notificationService;
            _permissionsGuard = permissionsGuard;
            _settingsManager = settingsManager;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<HealthReport> Handle(PublishHealthReportCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Started submitting health report with [Id]: {command.Id}");
            
            var report = await GetReportAsync(
                reportId: command.Id, 
                patientId: command.PatientId
            );
            
            _permissionsGuard.AssertPermissions(report);

            var reviewer = await _employeeService.GetByIdAsync(command.ReviewerId);

            if (!CanSubmitHealthReport(reviewer))
            {
                _logger.LogInformation($"Employee has no permission to submit report. Health report with [Id]: {command.Id} was sent to review.");

                await SendReportToReviewAsync(report);

                return report;
            }

            await _healthReportService.SubmitAsync(report, reviewer);
   
            _logger.LogInformation($"Health report with [Id]: {command.Id} submitted successfully");

            await SendHealthReportReviewedNotificationAsync(report, reviewer);

            _logger.LogInformation($"Health report notifications with [Id]: {command.Id} sent successfully");

            return report;
        }

        #region private

        /// <summary>
        /// Returns health report
        /// </summary>
        /// <param name="reportId"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<HealthReport> GetReportAsync(int reportId, int patientId)
        {
            var report = await _healthReportService.GetAsync(id: reportId, patientId: patientId);

            if (!report.CanBeSubmitted())
            {
                _logger.LogError($"Health report with [Id] = {report.Id} already submitted.");
                
                throw new AppException(HttpStatusCode.BadRequest, $"Can't publish corresponding health report.");
            }

            return report;
        }

        /// <summary>
        /// Checks if employee has permission to submit health report
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        private bool CanSubmitHealthReport(Employee employee)
        {
            return employee.Type == EmployeeType.Provider;
        }

        /// <summary>
        /// Sends report to review additionally notifying providers about it
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        private async Task SendReportToReviewAsync(HealthReport report)
        {
            var assignedEmployees = await _employeeService.GetAssignedToAsync(report.PatientId);

            var provider = assignedEmployees.FirstOrDefault(x => x.Type == EmployeeType.Provider);

            if (provider is null)
            {
                _logger.LogWarning($"Health report with [Id] = {report.GetId()} was not sending to review. Patient has no assigned provider.");
                
                throw new AppException(HttpStatusCode.BadRequest, "You must assign a provider before publishing");
            }
            
            await _healthReportService.SendToReviewAsync(report, provider);

            var notification = new HealthReportReviewNotification(provider, report);

            await _notificationService.CreateNotificationAsync(notification);        
        }

        /// <summary>
        /// Sends a health report reviewed notification to all involved users
        /// </summary>
        /// <param name="report"></param>
        /// <param name="reviewer"></param>
        /// <returns></returns>
        private async Task SendHealthReportReviewedNotificationAsync(HealthReport report, Employee reviewer)
        {
            var assignedEmployees = await _employeeService.GetAssignedToAsync(report.PatientId);

            var receivers = assignedEmployees.Where(x => x.GetId() != reviewer.GetId()).ToArray();

            if (receivers.Any())
            {
                var reportReviewedNotification = new HealthReportReviewedNotification(receivers, report);

                await _notificationService.CreateNotificationAsync(reportReviewedNotification);
            }

            var practiceId = report.Patient.User.PracticeId;

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];

            var healthReportUrl = string.Format(_options.HealthReportUrl, applicationUrl);

            var newHealthReportNotification = new NewHealthReportNotification(report.Patient, healthReportUrl);

            await _notificationService.CreateNotificationAsync(newHealthReportNotification);
        }

        #endregion
    }
}
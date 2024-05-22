using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Options;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class SendNewEnrollmentNotificationCommandHandler : IRequestHandler<SendNewEnrollmentNotificationCommand>
    {
        private static readonly PermissionType[] Permissions = {PermissionType.EnrollmentNotifications};
        
        private readonly INotificationService _notificationService;
        private readonly IEmployeeService _employeeService;
        private readonly IPatientsService _patientsService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISettingsManager _settingsManager;
        private readonly AppOptions _appOptions;

        public SendNewEnrollmentNotificationCommandHandler(
            INotificationService notificationService,
            IEmployeeService employeeService, 
            IPatientsService patientsService,
            IOptions<AppOptions> appOptions, 
            ISettingsManager settingsManager, 
            ISubscriptionService subscriptionService)
        {
            _notificationService = notificationService;
            _employeeService = employeeService;
            _patientsService = patientsService;
            _settingsManager = settingsManager;
            _subscriptionService = subscriptionService;
            _appOptions = appOptions.Value;
        }

        public async Task Handle(SendNewEnrollmentNotificationCommand command, CancellationToken cancellationToken)
        {
            var employees = await _employeeService.GetEmployeesByPermissionsAsync(
                permissions: Permissions,
                practiceIdId: command.PracticeId,
                locationId: command.LocationId);

            var patient = await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.NewEnrollmentNotificationSpecification);
            var patientProfileLink = await GetPatientProfileLink(patient);
            var subscription = await _subscriptionService.GetAsync(command.SubscriptionId, SubscriptionSpecifications.NewEnrollmentNotificationSpecification);
            var receivers = employees.Select(x => x.User).ToArray();

            await _notificationService.CreateNotificationAsync(new NewEnrollmentNotification(receivers, patient, subscription, patientProfileLink));
        }

        private async Task<string> GetPatientProfileLink(Patient patient)
        {
            var settings = await _settingsManager.GetSettings(new[] { SettingsNames.General.ApplicationBaseUrl }, patient.User.PracticeId);
            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
            var patientProfileLink = string.Format(_appOptions.PatientProfileUrl, applicationUrl, patient.Id);
            return patientProfileLink;
        }
    }
}
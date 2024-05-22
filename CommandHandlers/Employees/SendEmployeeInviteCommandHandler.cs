using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Options;
using MediatR;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.ConfirmCodes;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.CommandHandlers.Employees
{
    public class SendEmployeeInviteCommandHandler : IRequestHandler<SendEmployeeInviteCommand>
    {
        private const string EmailSubject = "Join {0}";

        private static readonly string[] EmailContainerSettings =
        {
            SettingsNames.General.ApplicationBaseUrl,
            SettingsNames.Emails.HeaderUrl,
            SettingsNames.Emails.LogoUrl,
            SettingsNames.Emails.WhiteLogoUrl,
            SettingsNames.Emails.WHLinkLogoUrl,
            SettingsNames.Emails.InstagramUrl,
            SettingsNames.Emails.WHInstagramLogoUrl
        };

        private readonly IAuthService _authService;
        private readonly IEmployeeService _employeeService;
        private readonly IEmailFactory _emailFactory;
        private readonly IEmailIntegrationService _emailIntegrationService;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly AppOptions _appOptions;
        private readonly IConfirmCodeService _confirmCodeService;

        public SendEmployeeInviteCommandHandler(
            IAuthService authService,
            IEmployeeService employeeService,
            IEmailFactory emailFactory,
            IEmailIntegrationService emailIntegrationService,
            IPracticeService practiceService,
            ISettingsManager settingsManager,
            IPermissionsGuard permissionsGuard,
            IOptions<AppOptions> appOptions,
            IConfirmCodeService confirmCodeService)
        {
            _employeeService = employeeService;
            _emailFactory = emailFactory;
            _emailIntegrationService = emailIntegrationService;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _permissionsGuard = permissionsGuard;
            _appOptions = appOptions.Value;
            _authService = authService;
            _confirmCodeService = confirmCodeService;
        }

        public async Task Handle(SendEmployeeInviteCommand command, CancellationToken cancellationToken)
        {
            var employee = await _employeeService.GetByIdAsync(command.EmployeeId);
            
            var location = employee.Locations.First().Location;

            _permissionsGuard.AssertPermissions(employee);

            var practiceId = employee.User.PracticeId;
            
            var practice = await _practiceService.GetOriginalPractice(practiceId);

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];

            var identity = await _authService.GetByEmailAsync(employee.User.Email);

            var confirmCode = await _confirmCodeService.GenerateAsync(identity.User, ConfirmCodeType.SetUpPassword);

            var link = string.Format(_appOptions.SetUpPasswordUrl, applicationUrl, confirmCode.Code);

            var data = new EmployeeInviteEmailModel
            {
                PracticeId = practice.Id,
                FirstName = employee.User.FirstName,
                JoinLink = link,
                LocationName = location.Name,
                PracticeName = practice.Name,
                PracticeEmail = practice.Email,
                ApplicationUrl = settings[SettingsNames.General.ApplicationBaseUrl],
                HeaderUrl = settings[SettingsNames.Emails.HeaderUrl],
                LogoUrl = settings[SettingsNames.Emails.LogoUrl],
                FooterLogoUrl = settings[SettingsNames.Emails.WhiteLogoUrl],
                WHLinkLogoUrl = settings[SettingsNames.Emails.WHLinkLogoUrl],
                WHInstagramLogoUrl = settings[SettingsNames.Emails.WHInstagramLogoUrl],
                InstagramUrl = settings[SettingsNames.Emails.InstagramUrl],
                PracticePhoneNumber = practice.PhoneNumber,
                PracticeAddress = $"{practice.Address.Address1} " +
                                  $"{practice.Address.City} " +
                                  $"{practice.Address.State} " +
                                  $"{practice.Address.ZipCode}"
            };

            var email = await _emailFactory.Create(new EmailDataModel<EmployeeInviteEmailModel>
            {
                Data = data
            });

            await _emailIntegrationService.SendEmailAndEventAsync(
                employee.User.Email, 
                string.Format(EmailSubject, location.Name), 
                email.Html, 
                employee.User.PracticeId,
                nameof(EmployeeInviteEmailModel),
                employee.User
            );
        }
    }
}
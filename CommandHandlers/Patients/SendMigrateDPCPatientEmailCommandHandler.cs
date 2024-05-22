using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Common.Options;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Application.Services.Practices;
using MediatR;
using WildHealth.Common.Constants;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class SendMigrateDPCPatientEmailCommandHandler : IRequestHandler<SendMigrateDPCPatientEmailCommand>
    {
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
        
        private readonly IEmailIntegrationService _emailIntegrationService;
        private readonly IEmailFactory _emailFactory;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly AppOptions _appOptions;

        public SendMigrateDPCPatientEmailCommandHandler(
            IEmailIntegrationService emailIntegrationService,
            IEmailFactory emailFactory,
            IPracticeService practiceService,
            ISettingsManager settingsManager,
            IOptions<AppOptions> appOptions)
        {
            _emailIntegrationService = emailIntegrationService;
            _emailFactory = emailFactory;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _appOptions = appOptions.Value;
        }

        public async Task Handle(SendMigrateDPCPatientEmailCommand command, CancellationToken cancellationToken)
        {
            var patient = command.Patient;
            var practiceId = patient.User.PracticeId;
            var host = _appOptions.ApiUrl;
            var practice = await _practiceService.GetOriginalPractice(patient.User.PracticeId);
            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);
            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
            var loginLink = string.Format(_appOptions.LoginUrl, applicationUrl);
            
            var email = await _emailFactory.Create(new EmailDataModel<MigrateDPCPatientModel>
            {
                Data = new MigrateDPCPatientModel
                {
                    Host = host,
                    FirstName = patient.User.FirstName,
                    LoginLink = loginLink,
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
                                      $"{practice.Address.ZipCode}",
                    PracticeId = practice.Id
                }
            });
            var subject = $"{practice.Name} Updates";

            await _emailIntegrationService.SendEmailAndEventAsync(
                patient.User.Email, 
                subject, 
                email.Html, 
                practiceId,
                nameof(MigrateDPCPatientModel),
                patient.User
            );
        }
    }
}

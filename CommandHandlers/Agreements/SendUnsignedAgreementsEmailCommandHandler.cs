using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Agreements;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Common.Options;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Application.Services.Practices;
using MediatR;
using WildHealth.Common.Constants;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Agreements
{
    public class SendUnsignedAgreementsEmailCommandHandler : IRequestHandler<SendUnsignedAgreementsEmailCommand>
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
        
        private readonly IEmailFactory _emailFactory;
        private readonly IEmailIntegrationService _emailIntegrationService;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly AppOptions _appOptions;

        public SendUnsignedAgreementsEmailCommandHandler(
            IEmailFactory emailFactory, 
            IEmailIntegrationService emailIntegrationService,
            IPracticeService practiceService,
            ISettingsManager settingsManager,
            IOptions<AppOptions> appOptions)
        {
            _emailFactory = emailFactory;
            _emailIntegrationService = emailIntegrationService;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _appOptions = appOptions.Value;
        }

        public async Task Handle(SendUnsignedAgreementsEmailCommand command, CancellationToken cancellationToken)
        {
            var patient = command.Patient;

            var practiceId = patient.User.PracticeId;
            
            var practice = await _practiceService.GetOriginalPractice(practiceId);

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var email = await _emailFactory.Create(new EmailDataModel<UnsignedAgreementsEmailModel>
            {
                Data = new UnsignedAgreementsEmailModel
                {
                    FirstName = patient.User.FirstName,
                    ConfirmAgreementsLink = _appOptions.ConfirmAgreementsUrl,
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
                    PracticeId = practiceId,
                }
            });

            var subject = $"Unsigned {practice.Name} Agreements";

            await _emailIntegrationService.SendEmailAndEventAsync(
                patient.User.Email, 
                subject, 
                email.Html, 
                patient.User.PracticeId, 
                nameof(UnsignedAgreementsEmailModel),
                patient.User
            );
        }
    }
}
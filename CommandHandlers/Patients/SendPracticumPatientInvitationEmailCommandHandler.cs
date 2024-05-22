using MediatR;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Application.Services.Fellows;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class SendPracticumPatientInvitationEmailCommandHandler: IRequestHandler<SendPracticumPatientInvitationEmailCommand>
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
        private readonly IFellowsService _fellowsService;
        private readonly AppOptions _appOptions;

        public SendPracticumPatientInvitationEmailCommandHandler(
            IEmailIntegrationService emailIntegrationService,
            IEmailFactory emailFactory,
            IPracticeService practiceService,
            ISettingsManager settingsManager,
            IFellowsService fellowsService,
            IOptions<AppOptions> appOptions)
        {
            _emailIntegrationService = emailIntegrationService;
            _emailFactory = emailFactory;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _fellowsService = fellowsService;
            _appOptions = appOptions.Value;
        }

        public async Task Handle(SendPracticumPatientInvitationEmailCommand command, CancellationToken cancellationToken)
        {
            var practiceId = command.Patient.User.PracticeId;
            
            var practice = await _practiceService.GetOriginalPractice(practiceId);

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var fellow = await _fellowsService.GetByIdAsync(command.FellowId);

            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
            
            var registrationLink = string.Format(_appOptions.FellowshipRegistrationUrl, applicationUrl);
            
            var email = await _emailFactory.Create(new EmailDataModel<PracticumPatientInvitationEmailModel>
            {
                Data = new PracticumPatientInvitationEmailModel
                {
                    PatientName = command.Patient.User.FirstName,
                    FellowName = $"{fellow.FirstName} {fellow.LastName}",
                    RegistrationLink = registrationLink,
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
                    PracticeId = practiceId
                }
            });

            var subject = "Wild Health Precision Medicine Education Program";

            await _emailIntegrationService.SendEmailAndEventAsync(
                command.Patient.User.Email, 
                subject, 
                email.Html, practice.Id,
                nameof(PracticumPatientInvitationEmailModel),
                command.Patient.User
            );
        }
    }
}


using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Schedulers;
using WildHealth.Application.Services.Emails;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Constants;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Settings;

namespace WildHealth.Application.EventHandlers.Schedulers
{
    public class SendInviteEmailOnSchedulerAccountCreatedEvent : INotificationHandler<SchedulerAccountCreatedEvent>
    {
        private static readonly string[] EmailContainerSettings =
        {
            SettingsNames.TimeKit.LoginLink,
            SettingsNames.General.ApplicationBaseUrl,
            SettingsNames.Emails.HeaderUrl,
            SettingsNames.Emails.LogoUrl,
            SettingsNames.Emails.WhiteLogoUrl,
            SettingsNames.Emails.WHLinkLogoUrl,
            SettingsNames.Emails.InstagramUrl,
            SettingsNames.Emails.WHInstagramLogoUrl
        };
        
        private readonly IEmailService _emailService;
        private readonly IPracticeService _practiceService;
        private readonly IEmailFactory _emailFactory;
        private readonly ISettingsManager _settingsManager;

        public SendInviteEmailOnSchedulerAccountCreatedEvent(
            IEmailService emailService,
            IPracticeService practiceService,
            IEmailFactory emailFactory,
            ISettingsManager settingsManager)
        {
            _emailService = emailService;
            _practiceService = practiceService;
            _emailFactory = emailFactory;
            _settingsManager = settingsManager;
        }
        public async Task Handle(SchedulerAccountCreatedEvent @event, CancellationToken cancellationToken)
        {
            var practiceId = @event.PracticeId;
            
            var practice = await _practiceService.GetOriginalPractice(practiceId);
            
            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var loginLink = settings[SettingsNames.TimeKit.LoginLink];

            var email = await _emailFactory.Create(new EmailDataModel<SchedulerInvitationModel>
            {
                Data = new SchedulerInvitationModel
                {
                    FirstName = @event.FirstName,
                    Passsword = @event.SchedulerPassword,
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
                    PracticeId = practiceId,
                }
            });

            await _emailService.SendAsync(
                to: @event.Email, 
                subject: "Scheduler invitation", 
                body: email.Html, 
                practiceId: @event.PracticeId
            );
        }
    }
}

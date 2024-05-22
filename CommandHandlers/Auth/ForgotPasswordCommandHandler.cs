using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using Microsoft.Extensions.Options;
using WildHealth.Application.Services.ConfirmCodes;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Common.Constants;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Auth
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
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
        
        private readonly IAuthService _authService;
        private readonly IConfirmCodeService _confirmCodeService;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly IEmailIntegrationService _emailIntegrationService;
        private readonly IEmailFactory _emailFactory;
        private readonly AppOptions _options;

        public ForgotPasswordCommandHandler(
            IAuthService authService,
            IConfirmCodeService confirmCodeService, 
            IPracticeService practiceService, 
            ISettingsManager settingsManager,
            IEmailIntegrationService emailIntegrationService, 
            IEmailFactory emailFactory, 
            IOptions<AppOptions> options)
        {
            _authService = authService;
            _confirmCodeService = confirmCodeService;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _emailIntegrationService = emailIntegrationService;
            _emailFactory = emailFactory;
            _options = options.Value;
        }

        public async Task Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
        {
            var identity = await _authService.GetByEmailAsync(command.Email);
            
            AssertIdentityIsActive(identity);

            await AssertPracticeAccess(command.PracticeId, identity);

            var confirmCode = await _confirmCodeService.GenerateAsync(identity.User, ConfirmCodeType.RestorePassword);
            
            await SendRestorePasswordEmail(identity, confirmCode.Code);
        }
        
        #region private
        
        /// <summary>
        /// Asserts identity is active
        /// </summary>
        /// <param name="identity"></param>
        private void AssertIdentityIsActive(UserIdentity identity)
        {
            if (!identity.IsActive())
            {
                throw new AppException(HttpStatusCode.Forbidden, "User is not active");
            }
        }
        
        /// <summary>
        /// Sends restore password email
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private async Task SendRestorePasswordEmail(UserIdentity identity, string code)
        {
            var practiceId = identity.PracticeId;
            
            var practice = await _practiceService.GetOriginalPractice(practiceId);
            
            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
            
            var link = string.Format(_options.RestorePasswordUrl, applicationUrl, code);

            var emailData = await _emailFactory.Create(new EmailDataModel<ResetPasswordEmailModel>
            {
                Data = new ResetPasswordEmailModel
                {
                    Link = link,
                    Name = identity.User.FirstName,
                    PracticeName = practice.Name,
                    PracticeEmail = practice.Email,
                    PracticePhoneNumber = practice.PhoneNumber,
                    ApplicationUrl = settings[SettingsNames.General.ApplicationBaseUrl],
                    HeaderUrl = settings[SettingsNames.Emails.HeaderUrl],
                    LogoUrl = settings[SettingsNames.Emails.LogoUrl],
                    FooterLogoUrl = settings[SettingsNames.Emails.WhiteLogoUrl],
                    WHLinkLogoUrl = settings[SettingsNames.Emails.WHLinkLogoUrl],
                    WHInstagramLogoUrl = settings[SettingsNames.Emails.WHInstagramLogoUrl],
                    InstagramUrl = settings[SettingsNames.Emails.InstagramUrl],
                    PracticeAddress = $"{practice.Address.Address1} " +
                                      $"{practice.Address.City} " +
                                      $"{practice.Address.State} " +
                                      $"{practice.Address.ZipCode}",
                    PracticeId = practiceId
                }
            });
  
            var subject = $"{practice.Name} Password Reset Request";
        

            await _emailIntegrationService.SendEmailAndEventAsync(
                identity.Email, 
                subject, 
                emailData.Html, 
                identity.User.PracticeId,
                nameof(ResetPasswordEmailModel),
                identity.User
            );
        }

        /// <summary>
        /// Assert practice access
        /// </summary>
        /// <param name="sourcePracticeId"></param>
        /// <param name="identity"></param>
        /// <returns></returns>
        private async Task AssertPracticeAccess(int sourcePracticeId, UserIdentity identity)
        {
            var hasSeparateUi = await _settingsManager.GetSetting<bool>(SettingsNames.General.HasSeparateUi, identity.PracticeId);
            var isSourcePracticeMain = await _settingsManager.GetSetting<bool>(SettingsNames.General.IsMainPractice, sourcePracticeId);

            if ((hasSeparateUi && sourcePracticeId != identity.PracticeId) || (!hasSeparateUi && !isSourcePracticeMain))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Access denied.");
            }
        }

        #endregion
    }
}
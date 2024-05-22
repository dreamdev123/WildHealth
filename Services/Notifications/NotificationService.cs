using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Domain.Entities.Notifications;
using WildHealth.Domain.Entities.Notifications.Abstracts;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.SMS;
using WildHealth.Common.Constants;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Settings;
using WildHealth.Shared.Data.Queries;
using WildHealth.Common.Options;
using Microsoft.Extensions.Options;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Services.Notifications
{
    /// <summary>
    /// <see cref="INotificationService"/>
    /// </summary>
    public class NotificationService : INotificationService
    {
        private static readonly string[] EmailContainerSettings =
        {
            SettingsNames.General.ApplicationBaseUrl,
            SettingsNames.Emails.HeaderUrl,
            SettingsNames.Emails.LogoUrl,
            SettingsNames.Emails.WhiteLogoUrl,
            SettingsNames.Emails.WHLinkLogoUrl,
            SettingsNames.Emails.InstagramUrl,
            SettingsNames.Emails.WHInstagramLogoUrl,
            SettingsNames.Emails.StaffEmail
        };
        
        private readonly IGeneralRepository<Notification> _notificationRepository;
        private readonly IEmailIntegrationService _emailService;
        private readonly IEmailFactory _emailFactory;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly ISMSService _smsService;
        private readonly ILogger<NotificationService> _logger;
        private readonly AppOptions _appOptions;

        public NotificationService(
            IGeneralRepository<Notification> notificationRepository,
            IEmailIntegrationService emailService,
            IEmailFactory emailFactory,
            IPracticeService practiceService, 
            ISMSService smsService,
            ISettingsManager settingsManager,
            ILogger<NotificationService> logger,
            IOptions<AppOptions> appOptions)
        {
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _emailFactory = emailFactory;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _smsService = smsService;
            _logger = logger;
            _appOptions = appOptions.Value;
        }

        /// <summary>
        /// <see cref="INotificationService.GetLastNotificationsAsync"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="lastNotificationId"></param>
        /// <returns></returns>
        public async Task<ICollection<Notification>> GetLastNotificationsAsync(int userId, int? lastNotificationId)
        {
            return await _notificationRepository
                .All()
                .RelatedTo(userId)
                .TakeAfter(lastNotificationId)
                .OrderByDescending(c=> c.CreatedAt)
                .AsNoTracking()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="INotificationService.CreateNotificationAsync(IBaseNotification)"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task CreateNotificationAsync(IBaseNotification model)
        {
            if (model is IClarityNotification clarityNotification)
            {
                await SaveNotificationsAsync(clarityNotification);
            }

            if (model is IEmailNotification emailNotification)
            {
                await SendEmailNotificationAsync(emailNotification);
            }
            if (model is ISMSNotification smsNotification)
            {
                if (smsNotification is IScheduledSmsNotificationWithParameters scheduledSmsNotification)
                {
                    await SendScheduledSmsNotification(scheduledSmsNotification);
                }
                else
                {
                    await SendSmsNotificationAsync(smsNotification);
                }
            }
        }

        /// <summary>
        /// <see cref="INotificationService.DeleteUserNotificationsAsync(int)"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task DeleteUserNotificationsAsync(int userId)
        {
            var notifications = await _notificationRepository.Get(c => c.UserId == userId).ToArrayAsync();

            _notificationRepository.Delete(notifications);
            await _notificationRepository.SaveAsync();
        }

        /// <summary>
        /// <see cref="INotificationService.DeleteUserNotificationsAsync(int,int[])"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task DeleteUserNotificationsAsync(int userId, int[] ids)
        {
            var notifications = await _notificationRepository
                .All()
                .ByIds(ids)
                .ToArrayAsync();

            foreach (var notification in notifications)
            {
                _notificationRepository.Delete(notification);
            }

            await _notificationRepository.SaveAsync();
        }

        /// <summary>
        /// <see cref="INotificationService.GetNotificationsAsync(IEnumerable{int?})"/>
        /// </summary>
        /// <param name="receiversIds"></param>
        /// <returns></returns>
        public async Task<ICollection<Notification>> GetNotificationsAsync(IEnumerable<int?> receiversIds)
        {
            return await _notificationRepository
                .All()
                .RelatedTo(receiversIds)
                .OrderByDescending(c=> c.CreatedAt)
                .AsNoTracking()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="INotificationService.DeleteUserNotificationAsync(int, int)"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="notificationId"></param>
        /// <returns></returns>
        public async Task DeleteUserNotificationAsync(int userId, int notificationId)
        {
            var notification = await _notificationRepository
                .Get(c => c.UserId == userId && c.Id == notificationId)
                .FirstOrDefaultAsync();

            if (notification is null) return;

            _notificationRepository.Delete(notification);
            await _notificationRepository.SaveAsync();
        }

        #region private

        private async Task SaveNotificationsAsync(IClarityNotification model)
        {
            if (!model.Users.Any())
            {
                return;
            }

            foreach (var user in model.Users)
            {
                await _notificationRepository.AddAsync(new Notification(
                    userId: user.Id, 
                    type: model.Type, 
                    subject: model.Subject, 
                    text: model.Text, 
                    linkData: model.LinkDataItems
                ));
            }

            await _notificationRepository.SaveAsync();
        }

        private async Task SendEmailNotificationAsync(IEmailNotification notification)
        {
            if (!notification.Users.Any() && !notification.IsStaffNotification)
            {
                return;
            }

            var practiceId = notification.PracticeId;

            var practice = await _practiceService.GetOriginalPractice(practiceId);

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];

            var email = await _emailFactory.Create(new EmailDataModel<NotificationEmailModel>
            {
                Data = new NotificationEmailModel(notification)
                {
                    PracticeName = practice.Name,
                    PracticeEmail = practice.Email,
                    PracticePhoneNumber = practice.PhoneNumber,
                    ApplicationUrl = applicationUrl,
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
                    PracticeId = practiceId,
                    ConversationUrl = string.Format(_appOptions.ConversationUrl, applicationUrl)
        }
            });

            var emails = notification.IsStaffNotification
                ? new []{settings[SettingsNames.Emails.StaffEmail]}
                : notification.Users.Select(x => x.Email);

            var subject = notification.Subject;

            var emailTemplateTypeName = notification.Type.ToString();

            await _emailService.BroadcastEmailAndEventAsync(
                to: emails, 
                subject: subject, 
                body: email.Html, 
                practiceId: practiceId, 
                emailTemplateTypeName: emailTemplateTypeName, 
                users: notification.Users,
                customArguments: new Dictionary<string, string>() { 
                    {"emailTemplateType", emailTemplateTypeName}
                }
            );
        }

        private async Task SendScheduledSmsNotification(IScheduledSmsNotificationWithParameters notification)
        {
            if (!notification.Users.Any())
            {
                _logger.LogError("There are no users in the ISMSNotification.");
                return;
            }

            if (string.IsNullOrEmpty(notification.SMSBody))
            {
                _logger.LogError("There is no body in the ISMSNotification.");
                return;
            }

            var hasTextParameters = notification.TextParameters != null && notification.TextParameters.Any();
            foreach (var user in notification.Users)
            {
                _logger.LogInformation($"Sending message {notification.SMSBody} to user id {user.Id} at practice {user.PracticeId}");
                try
                {
                    var smsBody = notification.SMSBody;
                    if (hasTextParameters)
                    {
                        smsBody = GetSmsBodyWithParameters(notification.TextParameters!, user, smsBody);
                    }
                    await _smsService.SendAsync(
                        messagingServiceSidType: SettingsNames.Twilio.MessagingServiceSid,
                        to: user.PhoneNumber, 
                        body: smsBody, 
                        universalId: user.UniversalId.ToString(), 
                        practiceId: user.PracticeId, 
                        sendAt: notification.SendAt);
                }
                catch (Exception error)
                {
                    _logger.LogError(
                        $"[Notification Service] Error sending message with this [Phone Number]: {user.PhoneNumber},  [User ID]: {user.Id} and [PracticeId] : {user.PracticeId} with error : {error.ToString()}");
                    throw new ApplicationException($"[Notification Service] Error sending message with this [Phone Number]: {user.PhoneNumber},  [User ID]: {user.Id} and [PracticeId] : {user.PracticeId}", error);
                }
            }
        }
        private async Task SendSmsNotificationAsync(ISMSNotification notification)
        {
            if (!notification.Users.Any())
            {
                _logger.LogError("There are no users in the ISMSNotification.");
                return;
            }

            if (string.IsNullOrEmpty(notification.SMSBody))
            {
                _logger.LogError("There is no body in the ISMSNotification.");
                return;
            }
            
            foreach (var user in notification.Users)
            {
                _logger.LogInformation($"Sending message {notification.SMSBody} to user id {user.Id} at practice {user.PracticeId}");
                try
                {
                    await _smsService.SendAsync(
                        messagingServiceSidType: notification.MessagingService ?? SettingsNames.Twilio.MessagingServiceSid, // MessagingServiceSid is the default messaging service
                        to: user.PhoneNumber, 
                        body: notification.SMSBody, 
                        universalId: user.UniversalId.ToString(), 
                        practiceId: user.PracticeId);
                }
                catch (Exception error)
                {
                    _logger.LogError(
                        $"[Notification Service] Error sending message with this [Phone Number]: {user.PhoneNumber},  [User ID]: {user.Id} and [PracticeId] : {user.PracticeId} with error : {error.ToString()}");
                    throw new ApplicationException($"[Notification Service] Error sending message with this [Phone Number]: {user.PhoneNumber},  [User ID]: {user.Id} and [PracticeId] : {user.PracticeId}", error);
                }
            }
        }

        private static string GetSmsBodyWithParameters(IEnumerable<string> textParameters, User user, string smsBodyTemplate)
        {
            foreach (var parameterName in textParameters)
            {
                            
                var parameterValue = user.GetType().GetProperty(parameterName)?.GetValue(user, null);
                if (parameterValue is not string value)
                {
                    throw new ArgumentException("The provided parameter is not accessible or is not of type string");
                }

                var stringToReplace = "{" + parameterName + "}";
                smsBodyTemplate = smsBodyTemplate.Replace(stringToReplace, value);
            }
            return smsBodyTemplate;
        }
        #endregion
    }
}

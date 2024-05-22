using System.Threading.Tasks;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using MessagingCredentials = WildHealth.Twilio.Clients.Credentials.CredentialsModel;


namespace WildHealth.Application.Services.Messaging.Base
{
    /// <summary>
    /// Provides base class for messaging services
    /// </summary>
    public abstract class MessagingBaseService
    {
        private readonly string[] _messagingSettingKeys = {
            SettingsNames.TwilioConversation.Url,
            SettingsNames.TwilioConversation.MediaUrl,
            SettingsNames.TwilioConversation.Username,
            SettingsNames.TwilioConversation.Password,
            SettingsNames.Twilio.AccountSid,
            SettingsNames.TwilioConversation.ApiKey,
            SettingsNames.TwilioConversation.ApiSecret,
            SettingsNames.TwilioConversation.ChatServiceId,
            SettingsNames.TwilioConversation.AccountSidConversation
        };

          private readonly string[] _smsSettingKeys = {
            SettingsNames.Twilio.AccountSid,
            SettingsNames.Twilio.AuthToken,
            SettingsNames.Twilio.SMSStatusUpdateWebhookUrl
        };

        private readonly ISettingsManager _settingsManager;

        protected MessagingBaseService(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected async Task<MessagingCredentials> GetMessagingCredentialsAsync(int practiceId)
        {
            var settings = await _settingsManager.GetSettings(_messagingSettingKeys, practiceId);

            return new MessagingCredentials(
                settings[SettingsNames.TwilioConversation.Url],
                settings[SettingsNames.TwilioConversation.MediaUrl],
                settings[SettingsNames.TwilioConversation.Username],
                settings[SettingsNames.TwilioConversation.Password],
                settings[SettingsNames.Twilio.AccountSid],
                settings[SettingsNames.TwilioConversation.ApiKey],
                settings[SettingsNames.TwilioConversation.ApiSecret],
                settings[SettingsNames.TwilioConversation.ChatServiceId],
                settings[SettingsNames.TwilioConversation.AccountSidConversation]
            );
        }
    }
}

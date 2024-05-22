using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.Services.SMS
{
    public class DummySMSService : ISMSService
    {
        private static readonly string[] SettingsKeys =
        {
            SettingsNames.Twilio.AccountSid,
            SettingsNames.Twilio.AuthToken,
            SettingsNames.Twilio.MessagingServiceSid,
            SettingsNames.Twilio.ApiUrl,
            SettingsNames.Twilio.SMSStatusUpdateWebhookUrl
        };
        
        private readonly ISettingsManager _settingsManager;
        private readonly ITwilioWebClient _twilioClient;
        private readonly ILogger<SMSService> _logger;
        private readonly IFeatureFlagsService _featureFlagsService;

        public DummySMSService(
            ISettingsManager settingsManager,
            ITwilioWebClient twilioClient,
            IFeatureFlagsService featureFlagsService,
            ILogger<SMSService> logger
            )
        {
            _settingsManager = settingsManager;
            _twilioClient = twilioClient;
            _featureFlagsService = featureFlagsService;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="ISMSService.SendAsync"/>
        /// </summary>
        /// <param name="messagingServiceSidType"></param>
        /// <param name="to">a phone number</param>
        /// <param name="body"></param>
        /// <param name="universalId"></param>
        /// <param name="practiceId"></param>
        /// <param name="avoidFlag"></param>
        /// <param name="sendAt"></param>
        public async Task<string> SendAsync(
            string messagingServiceSidType,
            string to,
            string body,
            string universalId,
            int practiceId,
            bool? avoidFlag = null,
            DateTime? sendAt = null)
        {
            return await Task.Run(() => {
                if (!_featureFlagsService.GetFeatureFlag("WH-SMS-Messaging"))
                {
                    return String.Empty;
                }

                var status = "Message sent"; 
                
                _logger.LogInformation($"Calling Dummy SMS service with [To]: {to}, [Body]: {body} and [PracticeId]: {practiceId}");
                
                return status; 
            }); 
        }

        public Task<string> SendAsyncNoFeatureFlag(
            string messagingServiceSidType,
            string to, 
            string body, 
            string universalId, 
            int practiceId)
        {
            var status = "Message sent avoiding feature flag";
            return Task.FromResult(status);
        }
    }
}
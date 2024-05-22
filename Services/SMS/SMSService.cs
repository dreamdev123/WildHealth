using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.PhoneLookupRecords;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Constants;
using WildHealth.Domain.Enums.Sms;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.SMS;
using WildHealth.IntegrationEvents.SMS.Payloads;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.Credentials;
using WildHealth.Twilio.Clients.Models.SMS;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.Services.SMS
{
    public class SMSService : ISMSService
    {
        private static readonly string[] SettingsKeys =
        {
            SettingsNames.Twilio.AccountSid,
            SettingsNames.Twilio.AuthToken,
            SettingsNames.Twilio.MessagingServiceSid,
            SettingsNames.Twilio.AppointmentReminderMessagingServiceSid,
            SettingsNames.Twilio.ApiUrl,
            SettingsNames.Twilio.SMSStatusUpdateWebhookUrl
        };

        private readonly ISettingsManager _settingsManager;
        private readonly ITwilioSmsWebClient _twilioSmsClient;
        private readonly ILogger<SMSService> _logger;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IPhoneLookupRecordService _phoneLookupRecordService;
        private readonly ISmsConsentService _smsConsentService;
        private readonly IEventBus _eventBus;
        private readonly IDateTimeProvider _dateTimeProvider;

        public SMSService(
            ISettingsManager settingsManager,
            ITwilioSmsWebClient twilioSmsClient,
            IFeatureFlagsService featureFlagsService,
            IPhoneLookupRecordService phoneLookupRecordService,
            ISmsConsentService smsConsentService,
            ILogger<SMSService> logger, 
            IEventBus eventBus, 
            IDateTimeProvider dateTimeProvider)
        {
            _settingsManager = settingsManager;
            _twilioSmsClient = twilioSmsClient;
            _featureFlagsService = featureFlagsService;
            _phoneLookupRecordService = phoneLookupRecordService;
            _smsConsentService = smsConsentService;
            _logger = logger;
            _eventBus = eventBus;
            _dateTimeProvider = dateTimeProvider;
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
            // avoidingFlag arg for bau process such as verification code
            if (avoidFlag.HasValue)
            {
                if (!avoidFlag.Value)
                {
                    _logger.LogInformation($"Explicitly avoid Flag coming as {avoidFlag.Value}, verifying...");

                    if (!_featureFlagsService.GetFeatureFlag("WH-SMS-Messaging"))
                    {
                        return string.Empty;
                    }
                }
            }
            else
            {
                if (!_featureFlagsService.GetFeatureFlag("WH-SMS-Messaging"))
                {
                    return string.Empty;
                }
            }

            var lookupResult = await _phoneLookupRecordService.GetOrCreateLookupAsync(new Guid(universalId), to);

            if (!lookupResult.SupportsSms())
            {
                //Any part of the app is welcome to use the phone lookup service.
                //We do this here as a last second check before sending.
                _logger.LogInformation($"The phone number {to} for universal id {universalId} does not support SMS.");
                return String.Empty;
            }
            

            var settings = await _settingsManager.GetSettings(SettingsKeys, practiceId);
            var sid = settings[SettingsNames.Twilio.AccountSid];
            var authToken = settings[SettingsNames.Twilio.AuthToken];
            var messagingServiceSid = settings[messagingServiceSidType];
            var apiUrl = settings[SettingsNames.Twilio.ApiUrl];
            var callbackUrlStatus = settings[SettingsNames.Twilio.SMSStatusUpdateWebhookUrl];
            var statusCallbackUrl = GetStatusCallbackUrl(callbackUrlStatus, universalId);

            var doNotMessage = await RevokedConsent(lookupResult.E164PhoneNumber, universalId, messagingServiceSid);
            if (doNotMessage)
            {
                //Any part of the app is welcome to use the consent service.
                //We do this here as a last second check before sending.
                _logger.LogInformation(
                    $"Skipping SMS: user with phone number {to} ({lookupResult.E164PhoneNumber}) no longer consents to SMS from service sid {messagingServiceSid}.");
                return "no-consent";
            }
            
            var creds = new CredentialsModel(sid, authToken, apiUrl);
            _twilioSmsClient.Initialize(creds);
            try
            {
                SMSRequestModel request;
                if (sendAt.HasValue)
                {
                    request = new ScheduledSMSRequestModel()
                    {
                        Body = body,
                        MessagingServiceSid = messagingServiceSid,
                        TwilioAccountSid = sid,
                        ToPhone = to,
                        StatusCallback = statusCallbackUrl,
                        SendAt = sendAt.Value,
                        ScheduleType = MessageResource.ScheduleTypeEnum.Fixed
                    };
                }
                else
                {
                    request = new SMSRequestModel()
                    {
                        Body = body,
                        MessagingServiceSid = messagingServiceSid,
                        TwilioAccountSid = sid,
                        ToPhone = to,
                        StatusCallback = statusCallbackUrl
                    };
                }

                try
                {
                    var result = await _twilioSmsClient.SendSMSAsync(sid, request);
                    
                    // Publish integration event
                    var eventPayload = new SMSSentPayload(result.Sid, result.Sid, result.AccountSid, result.MessagingServiceSid, result.From, to, body, result.Status, result.ErrorCode);
                    await _eventBus.Publish(new SMSIntegrationEvent(eventPayload, new UserMetadataModel(universalId), _dateTimeProvider.UtcNow()));
                    
                    return result.Status;
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                    throw;
                }
            }
            catch (TwilioException e)
            {
                //https://www.twilio.com/docs/api/errors/21211
                // TwilioException does not have Code definition
                // throw new AppException(HttpStatusCode.BadRequest, $"{to} is not a valid phone number");
                // throw new AppException(HttpStatusCode.InternalServerError, $"Error sending SMS for phone: {to}");
                //_logger.LogError($"Failure sending SMS message via Twilio with [code]: {e.Code} and message: {e.Message}");
                _logger.LogError($"Failure sending SMS message via Twilio: {e}");
                _logger.LogError($"Error sending SMS for phone: {to}");
                return string.Empty;
            }
        }

        private async Task<bool> RevokedConsent(string to, string universalId, string messagingServiceSid)
        {
            var consent = await _smsConsentService.GetAsync(to, messagingServiceSid);
            if (consent == null)
            {
                //This is the first message;
                return false;
            }
            
            if (consent.Setting == SmsConsentSetting.Allow)
            {
                return false;
            }
            
            //The user sent a STOP at some point.
            _logger.LogInformation($"Skipping SMS message to {to} ({universalId}) because of SmsConsentSetting for {messagingServiceSid}");
            return true;
        }

        private string GetStatusCallbackUrl(string callbackUrlStatus, string universalId)
        {
            //NOTE for testing status webhooks locally, you'll need to put your ngrok endpoint here.
            //Something like this:
            //return $"https://5d77-75-187-38-76.ngrok.io/twilio/sms/statusupdate/{universalId}";
            
            return $"{callbackUrlStatus}/{universalId}";
        }

        public async Task<string> SendAsyncNoFeatureFlag(
            string messagingServiceSidType,
            string to,
            string body,
            string universalId,
            int practiceId
        )
        {
            return await SendAsync(
                messagingServiceSidType: messagingServiceSidType,
                to: to, 
                body: body, 
                universalId: universalId, 
                practiceId: practiceId, 
                avoidFlag: true);
        }
    }
}
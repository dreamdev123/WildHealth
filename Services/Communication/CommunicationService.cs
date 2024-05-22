using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Common.Constants;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Users;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.SMS;
using WildHealth.Settings;

namespace WildHealth.Application.Services.Communication
{
    public class CommunicationService : ICommunicationService
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IPracticeService _practiceService;
        private readonly ILogger<CommunicationService> _logger;
        private static readonly string[] TwilioSettings =
        {
            SettingsNames.Twilio.AccountSid,
            SettingsNames.Twilio.AuthToken,
            //SettingsNames.Twilio.PhoneNumber,
        };
        private readonly string USCode = "+1"; //temporally used as prefix for all phone number
        private readonly ISMSService _smsService;

        public CommunicationService(
            ISettingsManager settingsManager,
            IPracticeService practiceService,
            ISMSService smsService,
            ILogger<CommunicationService> logger)
        {
            _settingsManager = settingsManager;
            _practiceService = practiceService;
            _smsService = smsService;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="ICommunicationService.SendVerificationSmsAsync"/>
        /// </summary>
        /// <param name="user"></param>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        public async Task SendVerificationSmsAsync(User user, string verificationCode)
        {
            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                _logger.LogError($"User with [Id] = {user.Id} does not have phone number.");
                throw new AppException(HttpStatusCode.BadRequest, "Phone number is empty.");
            }

            var settings = await GetSettingsAsync(user.PracticeId);

            var practice = await _practiceService.GetOriginalPractice(user.PracticeId);

            try
            {
                _logger.LogInformation($"Start sending verification sms to user with [Id] = {user.Id}.");
                await _smsService.SendAsyncNoFeatureFlag(
                    messagingServiceSidType: SettingsNames.Twilio.MessagingServiceSid,
                    to: user.PhoneNumber,
                    body: $"Your {practice.Name} Clarity verification code: {verificationCode}.",
                    universalId: user.UniversalId.ToString(),
                    practiceId: user.PracticeId
                );
                
            }
            catch(Exception e)
            {
                _logger.LogError($"Sending verification sms to user with [Id] = {user.Id} was failed. with this error {e.StackTrace}");
                throw new AppException(HttpStatusCode.InternalServerError, $"Error sending SMS for phone: {user.PhoneNumber}");
            }
        }

        #region private

        private async Task<IDictionary<string, string>> GetSettingsAsync(int practiceId)
        {
            return await _settingsManager.GetSettings(TwilioSettings, practiceId);
        }

        private string GetConvertedPhoneNumber(User user)
        {
            return USCode + user.PhoneNumber
                .Replace("(", "")
                .Replace(")", "")
                .Replace("-", "")
                .Replace(" ", "")
                .Trim();
        }

        #endregion
    }
}

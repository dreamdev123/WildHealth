using System;
using System.Net;
using System.Threading.Tasks;
using MassTransit.Initializers;
using Microsoft.Extensions.Logging;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;
using WildHealth.Domain.Constants;
using WildHealth.Settings;
using WildHealth.Shared.Exceptions;
using WildHealth.Twilio.Clients.Credentials;
using WildHealth.Twilio.Clients.Models.Lookup;
using WildHealth.Twilio.Clients.Models.SMS;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.Services.SMS
{
    public class SMSLookupService : ISMSLookupService
    {
        private static readonly string[] SettingsKeys =
        {
            SettingsNames.Twilio.AccountSid,
            SettingsNames.Twilio.AuthToken,
        };
        
        private static string LookupApiUrl = "https://lookups.twilio.com/";

        private readonly ISettingsManager _settingsManager;
        private readonly ITwilioSmsLookupWebClient _twilioLookupClient;
        private readonly ILogger<SMSLookupService> _logger;
        private readonly IFeatureFlagsService _featureFlagsService;
        private bool _initialized { get; set; }

        public SMSLookupService(
            ISettingsManager settingsManager,
            ITwilioSmsLookupWebClient twilioLookupClient,
            IFeatureFlagsService featureFlagsService,
            ILogger<SMSLookupService> logger
        )
        {
            _settingsManager = settingsManager;
            _twilioLookupClient = twilioLookupClient;
            _featureFlagsService = featureFlagsService;
            _logger = logger;
            _initialized = false;
        }

        private async Task Initialize()
        {
            var settings = await _settingsManager.GetSettings(SettingsKeys, 1);
            var sid = settings[SettingsNames.Twilio.AccountSid];
            var authToken = settings[SettingsNames.Twilio.AuthToken];
            var creds = new CredentialsModel(sid, authToken, LookupApiUrl);
            _twilioLookupClient.Initialize(creds);
            _initialized = true;
        }


        /// <summary>
        /// Looks up the E164 formatted phone number
        /// </summary>
        /// <param name="phoneNumber">e.g. +12223334444</param>
        /// <returns></returns>
        public async Task<LookupResponseModel> LookupE164Async(string phoneNumber)
        {
            if (!_initialized)
            {
                await Initialize();
            }
            var response = await _twilioLookupClient.LookupE164Async(phoneNumber);
            return response;
        }

        /// <summary>
        /// Looks up phone number formatted as a national format.
        /// e.g. for a US number, it can be something like (859) 111-2222
        /// And the country code provided should be "US"
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="countryCode"></param>
        /// <returns></returns>
        public async Task<LookupResponseModel> LookupNationalFormatAsync(string phoneNumber, string countryCode = "US")
        {
            if (!_initialized)
            {
                await Initialize();
            }

            var response = await _twilioLookupClient.LookupNationalFormatAsync(phoneNumber, countryCode);
            return response;
        }
    }
}
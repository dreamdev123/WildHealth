using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Settings;

namespace WildHealth.Application.Services.Messaging.Media
{
    /// <summary>
    /// <see cref="IMessagingMediaService"/>
    /// </summary>
    public class MessagingMediaService : MessagingBaseService, IMessagingMediaService
    {
        private readonly ITwilioMediaWebClient _twilioMediaClient;

        public MessagingMediaService(
            ITwilioMediaWebClient twilioMediaClient,
            ISettingsManager settingsManager) : base(settingsManager)
        {
            _twilioMediaClient = twilioMediaClient;
        }

        /// <summary>
        /// <see cref="IMessagingMediaService.RetrieveMediaResourceLinkAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="mediaSid"></param>
        /// <returns></returns>
        public async Task<string> RetrieveMediaResourceLinkAsync(int practiceId, string mediaSid)
        {
            var messagingCredentials = await GetMessagingCredentialsAsync(practiceId);

            _twilioMediaClient.Initialize(messagingCredentials);

            var fileLink = await _twilioMediaClient.RetrieveMediaResourceAsync(messagingCredentials.ChatServiceId, mediaSid);

            return fileLink.url;
        }

        /// <summary>
        /// <see cref="IMessagingMediaService.RetrieveMediaResourceLinksAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="mediaSid"></param>
        /// <returns></returns>
        public async Task<IDictionary<string, string>> RetrieveMediaResourceLinksAsync(int practiceId, string[] mediaSid)
        {
            var result = new Dictionary<string, string>();

            if (mediaSid is null || mediaSid.Any())
            {
                return result;
            }
            
            var messagingCredentials = await GetMessagingCredentialsAsync(practiceId);

            _twilioMediaClient.Initialize(messagingCredentials);

            var serviceId = messagingCredentials.ChatServiceId;

            var responses = mediaSid.Select(x => _twilioMediaClient.RetrieveMediaResourceAsync(serviceId, x)).ToArray();
            
            await Task.WhenAll(responses);

            return responses.Select(x => x.Result).ToDictionary(x => x.mediaId, x => x.url);
        }
    }
}

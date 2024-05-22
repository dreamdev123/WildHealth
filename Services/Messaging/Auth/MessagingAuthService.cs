using System.Threading.Tasks;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.Models.Tokens;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.Services.Messaging.Auth
{
    public class MessagingAuthService : MessagingBaseService, IMessagingAuthService
    {
        private readonly ITwilioWebClient _client;

        public MessagingAuthService(
            ITwilioWebClient client,
            ISettingsManager settingsManager) : base(settingsManager)
        {
            _client = client;
        }
        
        public async Task<AuthTokenModel> CreateConversationAuthToken(int practiceId, string identity)
        {
            var credentials = await GetMessagingCredentialsAsync(practiceId);

            _client.Initialize(credentials);

            return await _client.CreateConversationAuthTokenAsync(
                credentials.AccountSideConversation,
                credentials.ApiKey,
                credentials.ApiSecret,
                credentials.ChatServiceId,
                identity);
        }
    }
}

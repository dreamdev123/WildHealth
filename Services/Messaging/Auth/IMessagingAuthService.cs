using System.Threading.Tasks;
using WildHealth.Twilio.Clients.Models.Tokens;

namespace WildHealth.Application.Services.Messaging.Auth
{
    public interface IMessagingAuthService
    {
        /// <summary>
        /// Create conversation auth token for identity
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="identity"></param>
        /// <returns></returns>
        Task<AuthTokenModel> CreateConversationAuthToken(int practiceId, string identity);
    }
}

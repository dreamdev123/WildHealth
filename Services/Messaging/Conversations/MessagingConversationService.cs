using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.Models.ConversationParticipants;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Settings;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Shared.Exceptions;
using WildHealth.Twilio.Clients.Exceptions;

namespace WildHealth.Application.Services.Messaging.Conversations
{
    public class MessagingConversationService : MessagingBaseService, IMessagingConversationService
    {
        private readonly ITwilioWebClient _client;

        public MessagingConversationService(
            ITwilioWebClient client,
            ISettingsManager settingsManager) : base(settingsManager)
        {
            _client = client;
        }
        
        /// <summary>
        /// <see cref="IMessagingConversationService.CreateConversationAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="conversation"></param>
        /// <returns></returns>
        public async Task<ConversationModel> CreateConversationAsync(int practiceId, Conversation conversation)
        {
            _client.Initialize(await GetMessagingCredentialsAsync(practiceId));
            
            var friendlyName = conversation.Subject;
            
            var createConversationModel = new CreateConversationModel
            {
                FriendlyName = friendlyName
            };

            var messagingConversation = await _client.CreateConversationAsync(createConversationModel);

            return messagingConversation;
        }

        /// <summary>
        /// <see cref="IMessagingConversationService.CreateConversationParticipantAsync(int, Conversation, string)"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="conversation"></param>
        /// <param name="participant"></param>
        /// <returns></returns>
        public async Task<ConversationParticipantResponseModel> CreateConversationParticipantAsync(
            int practiceId,
            Conversation conversation,
            string messagingIdentity,
            string name)
        {
            _client.Initialize(await GetMessagingCredentialsAsync(practiceId));

            var conversationModel = new ConversationModel
            {
                Sid = conversation.VendorExternalId
            };

            var createConversationModel = new CreateConversationParticipantModel
            {
                Identity = messagingIdentity,
                FriendlyName = name
            };

            try
            {
                return await _client.CreateConversationParticipantAsync(conversationModel, createConversationModel);
            }
            catch (TwilioException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // If there's a conflict it means this participant is already active, let's find it and return it
                var participants =
                    await _client.GetConversationParticipantResourcesAsync(conversation.VendorExternalId);

                return participants.Participants.First(o => o.Identity == messagingIdentity);
            }
            
        }

        /// <summary>
        /// <see cref="IMessagingConversationService.RemoveConversationParticipantAsync(int, Conversation, string)"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="conversation"></param>
        /// <param name="vendorExternalId"></param>
        /// <returns></returns>
        public async Task RemoveConversationParticipantAsync(
            int practiceId,
            Conversation conversation,
            string vendorExternalId)
        {
            _client.Initialize(await GetMessagingCredentialsAsync(practiceId));

            var conversationModel = new ConversationModel
            {
                Sid = conversation.VendorExternalId
            };

            var createConversationModel = new CreateConversationParticipantModel
            {
                Identity = vendorExternalId
            };

            await _client.RemoveConversationParticipantAsync(conversationModel, createConversationModel);
        }
    }
}

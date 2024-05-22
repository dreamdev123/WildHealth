using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

using WildHealth.Domain.Entities.Conversations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// <see cref="IConversationParticipantMessageSentIndexService"/>
    /// </summary>
    public class ConversationParticipantMessageSentIndexService : IConversationParticipantMessageSentIndexService
    {
        private readonly IGeneralRepository<ConversationParticipantMessageSentIndex> _conversationParticipantMessageSentIndexRepository;

        public ConversationParticipantMessageSentIndexService(
            IGeneralRepository<ConversationParticipantMessageSentIndex> conversationParticipantMessageSentIndexRepository
        )
        {
            _conversationParticipantMessageSentIndexRepository = conversationParticipantMessageSentIndexRepository;
        }

        /// <summary>
        /// <see cref="IConversationParticipantMessageSentIndexService.GetByConversationAndParticipantAsync(string, string)"/>
        /// </summary>
        /// <param name="conversationVendorExternalId"></param>
        /// <param name="participantVendorExternalId"></param>
        /// <returns>ConversationParticipantMessageSentIndex</returns>
        public async Task<ConversationParticipantMessageSentIndex?> GetByConversationAndParticipantAsync(
            string conversationVendorExternalId, string participantVendorExternalId)
        {
            var model = await _conversationParticipantMessageSentIndexRepository
                .All()
                .ByConversationVendorExternalId(conversationVendorExternalId)
                .ByParticipantVendorExternalId(participantVendorExternalId)
                .FirstOrDefaultAsync();

            return model;
        }
    }
}

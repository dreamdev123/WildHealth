using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// <see cref="IConversationParticipantPatientService"/>
    /// </summary>
    public class ConversationParticipantPatientService : IConversationParticipantPatientService
    {
        private readonly IGeneralRepository<ConversationParticipantPatient> _conversationParticipantPatientRepository;

        public ConversationParticipantPatientService(IGeneralRepository<ConversationParticipantPatient> conversationParticipantPatientRepository)
        {
            _conversationParticipantPatientRepository = conversationParticipantPatientRepository;
        }

        /// <summary>
        /// <see cref="IConversationParticipantPatientService.GetByVendorExternalIdentityAndConversationId(string, int)"/>
        /// </summary>
        /// <param name="vendorExternalIdentity"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        public async Task<ConversationParticipantPatient?> GetByVendorExternalIdentityAndConversationId(
            string vendorExternalIdentity, int conversationId)
        {
            return await _conversationParticipantPatientRepository
                .All()
                .ByConversationId(conversationId)
                .ByVendorExternalIdentity(vendorExternalIdentity)
                .IncludeRelations()
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Returns all entries that do not have a vendorExternalId
        /// </summary>
        /// <returns></returns>
        public async Task<ConversationParticipantPatient[]> GetHealthParticipantsWithoutExternalId()
        {
            return await _conversationParticipantPatientRepository
                .All()
                .Where(o => string.IsNullOrEmpty(o.VendorExternalId) && o.Conversation.Type == ConversationType.HealthCare)
                .Include(o => o.Conversation)
                .Include(o => o.Patient).ThenInclude(o => o.User)
                .ToArrayAsync();
        }

    }
}

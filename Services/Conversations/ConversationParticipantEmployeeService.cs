using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// <see cref="IConversationParticipantPatientService"/>
    /// </summary>
    public class ConversationParticipantEmployeeService : IConversationParticipantEmployeeService
    {
        private readonly IGeneralRepository<ConversationParticipantEmployee> _conversationParticipantEmployeeRepository;

        public ConversationParticipantEmployeeService(IGeneralRepository<ConversationParticipantEmployee> conversationParticipantEmployeeRepository)
        {
            _conversationParticipantEmployeeRepository = conversationParticipantEmployeeRepository;
        }

        /// <summary>
        /// Returns all entries that do not have a vendorExternalId
        /// </summary>
        /// <returns></returns>
        public async Task<ConversationParticipantEmployee[]> GetHealthParticipantsWithoutExternalId()
        {
            return await _conversationParticipantEmployeeRepository
                .All()
                .Where(o => string.IsNullOrEmpty(o.VendorExternalId) && o.Conversation.Type == ConversationType.HealthCare)
                .Include(o => o.Conversation)
                .Include(o => o.Employee).ThenInclude(o => o.User)
                .ToArrayAsync();
        }

    }
}
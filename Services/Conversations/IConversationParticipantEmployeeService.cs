using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// Provides methods for working with table ConversationParticipantEmployeeService
    /// </summary>
    public interface IConversationParticipantEmployeeService
    {
        Task<ConversationParticipantEmployee[]> GetHealthParticipantsWithoutExternalId();
    }
}
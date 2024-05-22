using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Services.Conversations;

public interface IConversationTemplatesService
{
    Task<ConversationTemplate> GetAsync(int id);
    
    Task<ConversationTemplate[]> GetAsync(
        int? employeeId,
        ConversationType? conversationType,
        UserType userType);
}
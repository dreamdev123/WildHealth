using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Services.Conversations;

public class ConversationTemplatesService : IConversationTemplatesService
{
    private readonly IGeneralRepository<ConversationTemplate> _conversationTemplates;

    public ConversationTemplatesService(IGeneralRepository<ConversationTemplate> conversationTemplates)
    {
        _conversationTemplates = conversationTemplates;
    }

    public Task<ConversationTemplate> GetAsync(int id)
    {
        return _conversationTemplates
            .All()
            .ById(id)
            .FindAsync();
    }

    public Task<ConversationTemplate[]> GetAsync(int? employeeId, ConversationType? conversationType, UserType userType)
    {
        return _conversationTemplates
            .All()
            .RelatedToEmployee(employeeId)
            .ByConversationType(conversationType)
            .ByUserType(userType)
            .AsNoTracking()
            .ToArrayAsync();
    }
}
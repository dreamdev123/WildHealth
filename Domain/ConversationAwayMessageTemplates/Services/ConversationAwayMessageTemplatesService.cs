using System.Threading.Tasks;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Domain.ConversationAwayMessageTemplates.Services;

public interface IConversationAwayMessageTemplatesService
{
    Task<ConversationAwayMessageTemplate> GetById(int requestId);
}

public class ConversationAwayMessageTemplatesService : IConversationAwayMessageTemplatesService
{
    private readonly IGeneralRepository<ConversationAwayMessageTemplate> _repository;

    public ConversationAwayMessageTemplatesService(IGeneralRepository<ConversationAwayMessageTemplate> repository)
    {
        _repository = repository;
    }

    public async Task<ConversationAwayMessageTemplate> GetById(int id)
    {
        return await _repository.All()
            .ById(id)
            .FindAsync();
    }
}
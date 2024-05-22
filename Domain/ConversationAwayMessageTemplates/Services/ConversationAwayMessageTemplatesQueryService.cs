using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Common.Models.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Domain.ConversationAwayMessageTemplates.Services;

public interface IConversationAwayMessageTemplatesQueryService
{
    public Task<List<ConversationAwayMessageTemplateModel>> GetAll();
    public Task<ConversationAwayMessageTemplateModel> GetById(int id);
    Task<List<ConversationAwayMessageTemplateModel>> GetActive();
}

public class ConversationAwayMessageTemplatesQueryService : IConversationAwayMessageTemplatesQueryService
{
    private readonly IGeneralRepository<ConversationAwayMessageTemplate> _repository;

    public ConversationAwayMessageTemplatesQueryService(IGeneralRepository<ConversationAwayMessageTemplate> repository)
    {
        _repository = repository;
    }

    public async Task<List<ConversationAwayMessageTemplateModel>> GetAll()
    {
        return await Query().ToListAsync();
    }

    public async Task<ConversationAwayMessageTemplateModel> GetById(int id)
    {
        return await Query().FindAsync(x => x.Id == id);
    }

    public async Task<List<ConversationAwayMessageTemplateModel>> GetActive()
    {
        return await Query()
            .Where(x => x.IsActive)
            .ToListAsync();
    }
    
    private IQueryable<ConversationAwayMessageTemplateModel> Query()
    {
        return _repository.All()
            .Select(x => new ConversationAwayMessageTemplateModel
            {
                Id = x.Id!.Value,
                Title = x.Title,
                Body = x.Body,
                IsActive = !x.DeletedAt.HasValue
            });
    }
}
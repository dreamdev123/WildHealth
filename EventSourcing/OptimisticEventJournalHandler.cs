using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.EventSourcing;
using WildHealth.Infrastructure.Data.EntityStateManager;

namespace WildHealth.Application.EventSourcing;

public class OptimisticEventJournalHandler : IEventJournalHandler
{
    private readonly IEntityStateManager _entityStateManager;
    private const int MaxAttempts = 10;

    public OptimisticEventJournalHandler(IEntityStateManager entityStateManager)
    {
        _entityStateManager = entityStateManager;
    }

    public async Task Handle(EventJournal journalEvent)
    {
        var (saveFailed, attempt) = (false, 0);
        do
        {
            try
            {
                attempt++;
                saveFailed = false;
                await HandleCore(journalEvent);
            }
            catch (DbUpdateConcurrencyException e)
            {
                saveFailed = true;
                await e.Entries.Single().ReloadAsync();
            }
        } while (saveFailed && attempt <= MaxAttempts);
    }

    private async Task HandleCore(EventJournal journalEvent)
    {
        var entity = await _entityStateManager.FindUnsafeAsync(journalEvent.GetEntityType(), journalEvent.EntityId);
        journalEvent.Reduce(entity);
        _entityStateManager.Edit(entity);
        await _entityStateManager.SaveChanges();
    }
}
using System.Threading.Tasks;
using WildHealth.EventSourcing;

namespace WildHealth.Application.EventSourcing;

public interface IEventJournalHandler
{
    Task Handle(EventJournal journalEvent);
}
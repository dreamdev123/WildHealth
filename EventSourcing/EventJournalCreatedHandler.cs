using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Polly;
using Polly.Retry;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Models.Exceptions;
using WildHealth.EventSourcing;
using WildHealth.Infrastructure.Data.EntityStateManager;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.EventSourcing;

public record EventJournalCreated(int EventJournalId) : INotification;

public class EventJournalCreatedHandler : INotificationHandler<EventJournalCreated>
{
    private readonly IGeneralRepository<EventJournal> _eventJournalRepository;
    private readonly IEntityStateManager _entityStateManager;
    private static readonly AsyncRetryPolicy RetryPolicy = Policy.Handle<EntityNotFoundException>()
        .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5) });
    
    public EventJournalCreatedHandler(
        IGeneralRepository<EventJournal> eventJournalRepository, 
        IEntityStateManager entityStateManager)
    {
        _eventJournalRepository = eventJournalRepository;
        _entityStateManager = entityStateManager;
    }

    public async Task Handle(EventJournalCreated notification, CancellationToken cancellationToken)
    {
        // If a journal event created in a transaction which is not committed 
        // at the moment then the record will not be available for other sessions. 
        // That's why we retry to fetch the entity here.
        var journalEvent = await RetryPolicy.ExecuteAsync(async () => 
            await _eventJournalRepository.All().ById(notification.EventJournalId).FindAsync());
        
        var strategy = journalEvent.GetConcurrencyStrategy();
        var handler = GetHandler(strategy);

        await handler.Handle(journalEvent);
    }

    private IEventJournalHandler GetHandler(ConcurrencyStrategy strategy) => strategy switch
    {
        ConcurrencyStrategy.Optimistic => new OptimisticEventJournalHandler(_entityStateManager),
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
    };
}
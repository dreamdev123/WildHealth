using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Services.Emails;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Constants;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Infrastructure.EmailFactory.Models.SyncRecords;
using WildHealth.IntegrationEvents.FormIntegrations;
using WildHealth.IntegrationEvents.FormIntegrations.Payloads;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class OptOutOfDorothySubscriptionCommandHandler : IRequestHandler<OptOutOfDorothySubscriptionCommand>
{
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IEventBus _eventBus;

    public OptOutOfDorothySubscriptionCommandHandler(
        ISyncRecordsService syncRecordsService,
        IEventBus eventBus)
    {
        _syncRecordsService = syncRecordsService;
        _eventBus = eventBus;
    }

    public async Task Handle(OptOutOfDorothySubscriptionCommand command, CancellationToken cancellationToken)
    {
        var syncRecord = await _syncRecordsService.GetById<SyncRecordDorothy>(command.SyncRecordId);
        
        if (syncRecord is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Sync record id {command.SyncRecordId} not found");
        }

        syncRecord.SubscriptionOptIn = false;

        await _syncRecordsService.UpdateAsync(syncRecord);

        var payload = new FormOptOutPayload(
            syncRecord.SyncRecord.UniversalId,
            formType: SyncRecordConstants.FormTypeBySyncRecordType[syncRecord.SyncRecordType],
            reason: string.Empty);

        await _eventBus.Publish(new DorothyFormIntegrationEvent(
                payload: payload,
                eventDate: DateTime.UtcNow),
            cancellationToken: cancellationToken);
    }
}
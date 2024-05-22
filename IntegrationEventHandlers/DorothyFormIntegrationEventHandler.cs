using WildHealth.Infrastructure.Communication.MessageBus;
using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Entities.SyncRecords;
using WildHealth.IntegrationEvents.FormIntegrations;
using WildHealth.IntegrationEvents.FormIntegrations.Payloads;
using AutoMapper;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.SyncRecords;


namespace WildHealth.Application.IntegrationEventHandlers;

public class DorothyFormIntegrationEventHandler : IEventHandler<DorothyFormIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<DorothyFormIntegrationEventHandler> _logger;
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;

    public DorothyFormIntegrationEventHandler(IMediator mediator, 
        ILogger<DorothyFormIntegrationEventHandler> logger,
        IMapper mapper,
        ISyncRecordsService syncRecordsService,
        IEventBus eventBus)
    {
        _mediator = mediator;
        _logger = logger;
        _mapper = mapper;
        _syncRecordsService = syncRecordsService;
        _eventBus = eventBus;
    }

    public async Task Handle(DorothyFormIntegrationEvent @event)
    {
        _logger.LogInformation($"Event received {@event}");

        switch (@event.PayloadType)
        {
            case nameof(DorothyFormSubmittedPayload):
                await HandleDorothyFormSubmitted(@event.DeserializeIntegrationEventPayload<DorothyFormSubmittedPayload>());
                break;
            case nameof(FormRecordedPayload):
                await HandleFormRecorded(@event.DeserializeIntegrationEventPayload<FormRecordedPayload>());
                break;
            case nameof(FormOptOutPayload):
                await HandleFormOptOut(@event.DeserializeIntegrationEventPayload<FormOptOutPayload>());
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unknown dorothy form payload type of {@event.PayloadFullType}");
        }
    }

    #region private

    private async Task<SyncRecord> HandleDorothyFormSubmitted(DorothyFormSubmittedPayload payload)
    {
        var syncRecordDorothy = payload.ToSyncRecordType<SyncRecordDorothy>(_mapper);

        var record = await _syncRecordsService.CreateAsync<SyncRecordDorothy>(syncRecordDorothy, SyncRecordType.Dorothy, payload.PracticeId);

        await _eventBus.Publish(new DorothyFormIntegrationEvent(
            payload: new FormRecordedPayload(
                submissionId: syncRecordDorothy.SubmissionId,
                formId: syncRecordDorothy.FormId,
                formType: SyncRecordConstants.FormTypeBySyncRecordType[SyncRecordType.Dorothy]),
            eventDate: DateTime.UtcNow));
        
        return record.SyncRecord;
    }

    private async Task HandleFormRecorded(FormRecordedPayload payload)
    {
        switch (payload.FormType)
        {
            case FormConstants.Types.PheCovidTest:
                await _mediator.Send(new SendDorothyFormReceivedCommsCommand(payload.SubmissionId));
                break;
        }
    }
    
    private async Task HandleFormOptOut(FormOptOutPayload payload)
    {
        switch (payload.FormType)
        {
            case FormConstants.Types.PheCovidTest:
                await _mediator.Send(new SendDorothyOptOutCommsCommand(payload.UniversalId));
                break;
        }
    }

    #endregion
    

}
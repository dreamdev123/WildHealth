using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Application.Utils.Spreadsheets;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.SyncRecords;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.Messages;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class ConsumeFhirPaymentsPostedCommandHandler : IRequestHandler<ConsumeFhirPaymentsPostedCommand>
{
    private const int DEFAULT_PRACTICE = 1;
    
    private const string SyncDatumRemitId = "RemitId";
    
    private const string EraTitle = "era";
    private const string RemitIdTitle = "remit_id";
    private const string ReceiptIdTitle = "receipt_id";
    private const string PatientIdTitle = "patient_id";
    private const string EncounterIdTitle = "encounter_id";
    private const string ClaimStatusTitle = "claim_status";
    private const string ApplyStatusTitle = "apply_status";
    private const string ReceivedAmountTitle = "received_amount";
    private const string AppliedAmountTitle = "applied_amount";
    private const string PostedAtTitle = "posted_at";
    
    private readonly ILogger<ConsumeFhirPaymentsPostedCommandHandler> _logger;
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IEventBus _eventBus;
    private readonly IMapper _mapper;

    public ConsumeFhirPaymentsPostedCommandHandler(
        ILogger<ConsumeFhirPaymentsPostedCommandHandler> logger,
        ISyncRecordsService syncRecordsService,
        IEventBus eventBus,
        IMapper mapper)
    {
        _logger = logger;
        _syncRecordsService = syncRecordsService;
        _eventBus = eventBus;
        _mapper = mapper;
    }

    public async Task Handle(ConsumeFhirPaymentsPostedCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Attempting to consume file for fhir payments posted");

        var spreadsheetIterator = new SpreadsheetIterator(command.File);
        
        var importantTitles = new Dictionary<string, string>
        {
            { EraTitle, string.Empty },
            { RemitIdTitle, string.Empty },
            { ReceiptIdTitle, string.Empty },
            { PatientIdTitle, string.Empty },
            { EncounterIdTitle, string.Empty },
            { ClaimStatusTitle, string.Empty },
            { ApplyStatusTitle, string.Empty },
            { ReceivedAmountTitle, string.Empty },
            { AppliedAmountTitle, string.Empty },
            { PostedAtTitle, string.Empty }
        };
        
        try
        {
            await spreadsheetIterator.Iterate(importantTitles, async (rowResults) =>
            {
                var era = rowResults[EraTitle];
                var remitId = rowResults[RemitIdTitle];
                var receiptId = rowResults[ReceiptIdTitle];
                var patientId = rowResults[PatientIdTitle];
                var encounterId = rowResults[EncounterIdTitle];
                var claimStatus = rowResults[ClaimStatusTitle];
                var applyStatus = rowResults[ApplyStatusTitle];
                var receivedAmount = rowResults[ReceivedAmountTitle];
                var appliedAmount = rowResults[AppliedAmountTitle];
                var postedAt = rowResults[PostedAtTitle];
                
                var keys = new Dictionary<string, string>()
                {
                    {SyncDatumRemitId, remitId}
                };
                
                var syncRecords = await _syncRecordsService.GetByKeys<SyncRecordFhirRemit>(keys);

                if (syncRecords.Any())
                {
                    return;
                }

                var record = new SyncRecordFhirRemit
                {
                    RemitId = remitId,
                    ReceiptId = receiptId,
                    PatientId = patientId,
                    EncounterId = encounterId,
                    ClaimStatus = Convert.ToInt32(claimStatus),
                    ApplyStatus = applyStatus,
                    ReceivedAmount = Convert.ToDecimal(receivedAmount),
                    AppliedAmount = Convert.ToDecimal(appliedAmount),
                    PostedDate = DateTime.TryParse(postedAt, out var postedDate) ? postedDate : new DateTime()
                };

                await _syncRecordsService.CreateAsync(record, SyncRecordType.FhirRemit, DEFAULT_PRACTICE);
                
                await _eventBus.Publish(new FhirPaymentIntegrationEvent(
                    payload: _mapper.Map<FhirPaymentPostedPayload>(record),
                    eventDate: DateTime.UtcNow));
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to consume fhir payment posted, {ex}");

            throw;
        }
        
        _logger.LogInformation($"Finished consuming file for fhir payments posted");
    }
}
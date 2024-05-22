using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Insurances;
using WildHealth.Domain.Enums.Insurance;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class HandleRemitFileProcessedCommandHandler : IRequestHandler<HandleRemitFileProcessedCommand>
{
    private readonly IRemitFilesService _remitFilesService;
    private readonly IClaimsService _claimsService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<HandleRemitFileProcessedCommandHandler> _logger;

    public HandleRemitFileProcessedCommandHandler(
        IRemitFilesService remitFilesService,
        IClaimsService claimsService,
        IEventBus eventBus,
        ILogger<HandleRemitFileProcessedCommandHandler> logger)
    {
        _remitFilesService = remitFilesService;
        _claimsService = claimsService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(HandleRemitFileProcessedCommand command, CancellationToken cancellationToken)
    {
        var remitFile = await _remitFilesService.GetByIdAsync(command.RemitFileId);
        
        foreach (var remit in remitFile.Remits)
        {
            var claim = remit.Claim;

            var newClaimStatus = remit.GetClaimStatus();

            var syncRecord = claim.ClaimantSyncRecord;
            
            await _eventBus.Publish(new FhirClaimIntegrationEvent(
                payload: new FhirClaimStatusChangedPayload(
                    id: claim.GetId(),
                    practiceId: remitFile.PracticeId,
                    newStatusId: Convert.ToInt32(newClaimStatus),
                    entity: String.Empty,
                    category: String.Empty,
                    statusCode: String.Empty,
                    date: DateTime.UtcNow
                ),
                user: new UserMetadataModel(syncRecord.UniversalId.ToString()),
                eventDate: DateTime.UtcNow
            ), cancellationToken);
        }
    }
}
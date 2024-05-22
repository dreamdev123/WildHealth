using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Insurances;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.ClaimStatus;
using WildHealth.Integration.Services;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;
using ClaimStatus = WildHealth.Domain.Enums.Insurance.ClaimStatus;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class SyncInsuranceClaimStatusUpdatesCommandHandler : IRequestHandler<SyncInsuranceClaimStatusUpdatesCommand>
{
    private readonly IClearinghouseIntegrationServiceFactory _clearinghouseIntegrationServiceFactory;
    private readonly IClaimStatusFilesService _claimStatusFilesService;
    private readonly IClaimsService _claimsService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SyncInsuranceClaimStatusUpdatesCommandHandler> _logger;

    public SyncInsuranceClaimStatusUpdatesCommandHandler(
        IClearinghouseIntegrationServiceFactory clearinghouseIntegrationServiceFactory,
        IClaimStatusFilesService claimStatusFilesService,
        IClaimsService claimsService,
        IEventBus eventBus,
        ILogger<SyncInsuranceClaimStatusUpdatesCommandHandler> logger)
    {
        _clearinghouseIntegrationServiceFactory = clearinghouseIntegrationServiceFactory;
        _claimStatusFilesService = claimStatusFilesService;
        _claimsService = claimsService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(SyncInsuranceClaimStatusUpdatesCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing of insurance claim status files has: started");
        
        var clearinghouseService = await _clearinghouseIntegrationServiceFactory.CreateAsync(command.PracticeId);

        var files = await clearinghouseService.GetClaimStatusFilesAsync(command.PracticeId);

        var orderedFiles = files.OrderBy(o => o.AddedDate);

        foreach (var claimStatusFile in orderedFiles)
        {
            try
            {
                var existingClaimStatusFile = await _claimStatusFilesService.GetByFileNameAsync(claimStatusFile.FileName);

                if (existingClaimStatusFile is not null)
                {
                    continue;
                }

                await ProcessClaimStatusFile(
                    claimStatusFile: claimStatusFile, 
                    clearinghouseService: clearinghouseService, 
                    practiceId: command.PracticeId);

                var newClaimStatusFile = new ClaimStatusFile(
                    fileName: claimStatusFile.FileName,
                    addedDate: claimStatusFile.AddedDate,
                    practiceId: command.PracticeId);
                
                await _claimStatusFilesService.CreateAsync(newClaimStatusFile);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Syncing of insurance claim status files has: failed to process file = {claimStatusFile.FileName} {ex}");
            }

        }
        
        _logger.LogInformation("Syncing of insurance claim status files has: finished");
    }

    #region private

    private async Task ProcessClaimStatusFile(
        ClaimStatusFileModel claimStatusFile,
        IClearinghouseIntegrationService clearinghouseService,
        int practiceId)
    {
        var claimStatuses = await clearinghouseService.GetClaimStatusFromFileAsync(
            fileName: claimStatusFile.FileName,
            practiceId: practiceId);
        
         var finalizedClaimStatuses = new[] { ClaimStatus.Paid, ClaimStatus.PartiallyPaid, ClaimStatus.Denied };

        foreach (var claimStatusModel in claimStatuses)
        {
            try
            {
                var claim = await _claimsService.GetById(ResolveClaimId(Convert.ToInt32(claimStatusModel.ClaimId)));

                if (finalizedClaimStatuses.Contains(claim.ClaimStatus))
                {
                    continue;
                }

                var newClaimStatus = GetClaimStatus(claimStatusModel);

                var payload = new FhirClaimStatusChangedPayload(
                    id: claim.GetId(),
                    practiceId: practiceId,
                    newStatusId: Convert.ToInt32(newClaimStatus),
                    entity: claimStatusModel.Entity,
                    category: claimStatusModel.Category,
                    statusCode: claimStatusModel.Status,
                    date: claimStatusModel.Date);

                await _eventBus.Publish(new FhirClaimIntegrationEvent(
                        payload: payload,
                        user: new UserMetadataModel(claim.ClaimantUniversalId.ToString()),
                        eventDate: DateTime.UtcNow));

            }
            catch (Exception e)
            {
                _logger.LogError($"Syncing of insurance claim status files has: failed to process status for claim id = {claimStatusModel.ClaimId} and file = {claimStatusFile.FileName} {e}");
            }
        }
    }
    
    private int ResolveClaimId(int claimId)
    {
        var manuallyEnteredClaims = new Dictionary<int, int>()
        {
            { 341969, 5715 },
            { 341970, 5716 },
            { 341971, 5717 },
            { 341975, 5718 },
            { 341976, 5719 },
            { 341979, 5720 },
            { 341980, 5721 },
            { 341984, 5722 },
            { 341987, 5723 }
        };

        return manuallyEnteredClaims.ContainsKey(claimId) ? manuallyEnteredClaims[claimId] : claimId;
    }

    private ClaimStatus GetClaimStatus(ClaimStatusModel model)
    {
        var rejectedCategories = new[] { "A3", "A4", "A5", "A6", "A7", "A8" };
        
        if (model.Category == "A1")
        {
            return ClaimStatus.ClearinghouseAccepted;
        }
        
        if (model.Category == "A2")
        {
            return ClaimStatus.PayerAccepted;
        }

        if (rejectedCategories.Contains(model.Category))
        {
            return model.Entity == "PR" ? ClaimStatus.PayerRejected : ClaimStatus.ClearinghouseRejected;
        }

        if (model.Category == "F0")
        {
            return ClaimStatus.Finalized;
        }
        
        if (model.Category == "F1")
        {
            return ClaimStatus.FinalizedPaid;
        }
        
        if (model.Category == "F2")
        {
            return ClaimStatus.FinalizedDenied;
        }

        return ClaimStatus.Unknown;

    }

    #endregion
}
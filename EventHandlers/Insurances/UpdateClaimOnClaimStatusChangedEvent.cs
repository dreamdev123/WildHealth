using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Events.Insurances;
using WildHealth.Application.Services.Insurances;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Insurances.EDI;
using WildHealth.Domain.Enums.Insurance;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;

namespace WildHealth.Application.EventHandlers.Insurances;

public class UpdateClaimOnClaimStatusChangedEvent : INotificationHandler<ClaimStatusChangedEvent>
{
    private readonly IMediator _mediator;
    private readonly IClaimsService _claimsService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UpdateClaimOnClaimStatusChangedEvent> _logger;

    public UpdateClaimOnClaimStatusChangedEvent(
        IMediator mediator,
        IClaimsService claimsService,
        IEventBus eventBus,
        ILogger<UpdateClaimOnClaimStatusChangedEvent> logger)
    {
        _mediator = mediator;
        _eventBus = eventBus;
        _claimsService = claimsService;
        _logger = logger;
    }
    
    public async Task Handle(ClaimStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Handling claim status changed event for [ClaimId] = {notification.ClaimId}");
        
        var claimStatus = (ClaimStatus) notification.NewStatusId;
        
        _logger.LogInformation($"Changing claim status to [ClaimStatus] = {claimStatus}");

        var claim = await _claimsService.GetById(notification.ClaimId);

        claim.ClaimStatus = claimStatus;
        
        claim.StatusHistory.Add(new WildHealth.Domain.Entities.Insurances.ClaimStatus(
            status: claimStatus,
            edi277: new Edi277()
            {
                Category = notification.Category,
                Entity = notification.Status,
                StatusCode = notification.StatusCode
            }
            ));
        
        _logger.LogInformation($"Saving claim status changed event for [ClaimId] = {notification.ClaimId}");

        await _claimsService.UpdateAsync(claim);
        
        if (claimStatus == ClaimStatus.Denied)
        {
            var payload = new FhirClaimDeniedPayload(
                id: claim.GetId(),
                practiceId: notification.PracticeId, 
                date: notification.Date);

            await _eventBus.Publish(new FhirClaimIntegrationEvent(
                    payload: payload,
                    user: new UserMetadataModel(claim.ClaimantUniversalId.ToString()),
                    eventDate: DateTime.UtcNow),
                cancellationToken);
        }
        else if (claimStatus == ClaimStatus.Paid || claimStatus == ClaimStatus.PartiallyPaid)
        {
            var payload = new FhirClaimPaidPayload(
                id: claim.GetId(),
                practiceId: notification.PracticeId, 
                date: notification.Date);
                
            await _eventBus.Publish(new FhirClaimIntegrationEvent(
                    payload: payload,
                    user: new UserMetadataModel(claim.ClaimantUniversalId.ToString()),
                    eventDate: DateTime.UtcNow),
                cancellationToken);
        }
        
        _logger.LogInformation($"Successfully updated claim status for [ClaimId] = {notification.ClaimId}");
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Ai;
using WildHealth.Application.Events.Ai;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Models.Extensions;
using MediatR;

namespace WildHealth.Application.EventHandlers.Ai;

public class AiMessageIntentOnOnMessageAddedEvent : INotificationHandler<AiConversationMessageAddedEvent>
{
    private readonly IMediator _mediator;
    private readonly IPatientsService _patientsService;
    private readonly IConversationsService _conversationsService;
        
    public AiMessageIntentOnOnMessageAddedEvent(
        IMediator mediator,
        IPatientsService patientsService,
        IConversationsService conversationsService)
    {
        _mediator = mediator;
        _patientsService = patientsService;
        _conversationsService = conversationsService;
    }
    
    public async Task Handle(AiConversationMessageAddedEvent notification, CancellationToken cancellationToken)
    {
        if (!await CanExecuteAiAssistForEvent(notification))
        {
            return;
        }
                
        var processMessageCommand = new MessageIntentCommand(
            conversationSid: notification.ConversationSid, 
            messageSid: notification.MessageSid,
            universalId: notification.UserUniversalId
        );
        
        await _mediator.Send(processMessageCommand, cancellationToken);
    }
    
    #region private
        
    private async Task<bool> CanExecuteAiAssistForEvent(AiConversationMessageAddedEvent e)
    {
        return await IsPatientMessage(e.UserUniversalId) && await IsHealthCareConversion(e.ConversationSid);
    }

    /// <summary>
    /// Validates that the message event was created by a patient
    /// </summary>
    private async Task<bool> IsPatientMessage(string universalId)
    {
        // Validate the UniversalId is valid and belongs to a Patient
        var patientOption = await _patientsService.GetByUserUniversalId(Guid.Parse(universalId)).ToOption();
        return patientOption.HasValue();
    }
        
    /// <summary>
    /// Validates that the message event was for health care conversation
    /// </summary>
    private async Task<bool> IsHealthCareConversion(string conversationId)
    {
        var conversationTry = await _conversationsService.GetByExternalVendorIdAsync(conversationId).ToTry();
        return conversationTry.Select(c => c.Type == ConversationType.HealthCare).ValueOr(false);
    }
        
    #endregion
}
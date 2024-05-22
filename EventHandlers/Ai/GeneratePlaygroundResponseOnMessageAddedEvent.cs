using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Ai;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Common.Constants;
using WildHealth.Common.Enums;
using MediatR;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.EventHandlers.Ai;

public class GeneratePlaygroundResponseOnMessageAddedEvent : INotificationHandler<AiConversationMessageAddedEvent>
{
    private readonly IMediator _mediator;
    private readonly IConversationsService _conversationsService;
    private readonly IFeatureFlagsService _featureFlagsService;

    public GeneratePlaygroundResponseOnMessageAddedEvent(
        IMediator mediator,
        IConversationsService conversationsService, 
        IFeatureFlagsService featureFlagsService)
    {
        _mediator = mediator;
        _conversationsService = conversationsService;
        _featureFlagsService = featureFlagsService;
    }

    public async Task Handle(AiConversationMessageAddedEvent notification, CancellationToken cancellationToken)
    {
        if (!await CanGeneratePlaygroundResponse(notification)) 
            return;
            
        var conversation = await _conversationsService.GetByExternalVendorIdAsync(notification.ConversationSid);
        
        var userUniversalId = GetUserUniversalId(conversation);
        
        var processMessageCommand = new GeneratePlaygroundResponseCommand(
            type: conversation.Type,
            conversationSid: notification.ConversationSid, 
            messageSid: notification.MessageSid,
            flowType: GetFlowType(),
            userUniversalId: userUniversalId
        );
            
        await _mediator.Send(processMessageCommand, cancellationToken);
    }
    
    #region private
        
    private async Task<bool> CanGeneratePlaygroundResponse(AiConversationMessageAddedEvent e)
    {
        return await IsPlaygroundConversion(e.ConversationSid);
    }

    private FlowType GetFlowType()
    {
        return _featureFlagsService.GetFeatureFlag(FeatureFlags.AsyncHcMessageAssist) 
            ? FlowType.Asynchronous 
            : FlowType.Regular;
    }
    
    /// <summary>
    /// Validates that the message event was for health care conversation
    /// </summary>
    private async Task<bool> IsPlaygroundConversion(string conversationId)
    {
        var conversationTry = await _conversationsService.GetByExternalVendorIdAsync(conversationId).ToTry();

        if (!conversationTry.HasValue()) return false;

        return ConversationDomain.Create(conversationTry.Value()).IsPlaygroundConversation();
    }

    private string GetUserUniversalId(Conversation conversation)
    {
        if (!conversation.PatientParticipants.Any())
        {
            return string.Empty;
        }

        var patient = conversation.PatientParticipants.First().Patient;

        return patient.User.UniversalId.ToString();
    }
        
    #endregion
}
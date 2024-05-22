using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Ai;
using WildHealth.Application.Events.Ai;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Constants;
using WildHealth.Common.Enums;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.EventHandlers.Ai
{
    public class HealthCoachAssistOnMessageAddedEvent : INotificationHandler<AiConversationMessageAddedEvent>
    {
        private readonly IMediator _mediator;
        private readonly IPatientsService _patientsService;
        private readonly IConversationsService _conversationsService;
        private readonly IFeatureFlagsService _featureFlagsService;
        
        public HealthCoachAssistOnMessageAddedEvent(
            IMediator mediator,
            IPatientsService patientsService,
            IConversationsService conversationsService,
            IFeatureFlagsService featureFlagsService)
        {
            _mediator = mediator;
            _patientsService = patientsService;
            _conversationsService = conversationsService;
            _featureFlagsService = featureFlagsService;
        }
        
        public async Task Handle(AiConversationMessageAddedEvent notification, CancellationToken cancellationToken)
        {
            if (!await CanExecuteAiAssistForEvent(notification)) 
                return;
                
            var processMessageCommand = new ConversationAiHcAssistCommand(
                conversationSid: notification.ConversationSid, 
                messageSid: notification.MessageSid,
                flowType: GetFlowType()
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
        
        private FlowType GetFlowType()
        {
            return _featureFlagsService.GetFeatureFlag(FeatureFlags.AsyncHcMessageAssist) 
                ? FlowType.Asynchronous 
                : FlowType.Regular;
        }
        
        #endregion
    }
}
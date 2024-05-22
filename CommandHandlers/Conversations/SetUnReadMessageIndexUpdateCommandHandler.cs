using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class SetUnreadMessageIndexUpdateCommandHandler : MessagingBaseService, IRequestHandler<SetUnreadMessageUpdateCommand, ConversationParticipantMessageReadIndex>
    {
        private readonly IConversationParticipantMessageReadIndexService _conversationParticipantMessageReadIndexService;
        private readonly IConversationsService _conversationsService;
        private readonly IMediator _mediator;
      

        public SetUnreadMessageIndexUpdateCommandHandler(
            IConversationParticipantMessageReadIndexService conversationParticipantMessageReadIndexService,
            IConversationsService conversationsService,
            ISettingsManager settingsManager,
            IMediator mediator) : base(settingsManager)
        {
            _conversationParticipantMessageReadIndexService = conversationParticipantMessageReadIndexService;
            _conversationsService = conversationsService;
            _mediator = mediator;
        }

        /// <summary>
        /// This handler will set unread index lower than last index created
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConversationParticipantMessageReadIndex> Handle(SetUnreadMessageUpdateCommand request, CancellationToken cancellationToken)
        {
            var conversation =
                await _conversationsService.GetByExternalVendorIdAsync(request.ConversationExternalVendorId);
            var model = await _conversationParticipantMessageReadIndexService.GetByConversationAndParticipantAsync(
                request.ConversationExternalVendorId,
                request.ParticipantExternalVendorId
            );
            
            if (model is null) 
            {
                model = ConversationParticipantMessageReadIndex.Create(
                        conversationId: conversation.GetId(),
                        conversationVendorExternalId: request.ConversationExternalVendorId, 
                        participantVendorExternalId: request.ParticipantExternalVendorId, 
                        lastReadIndex: request.LastMessageReadIndex,
                        participantIdentity: new Guid(request.ParticipantExternalVendorId));
                           
                await _conversationParticipantMessageReadIndexService.CreateAsync(model);
            } 
            else
            {
                model.SetLastMessageReadIndex(request.LastMessageReadIndex);
                await _conversationParticipantMessageReadIndexService
                        .UpdateAsync(model);
            }
            
            var unreadMessagesCount = conversation.Index - request.LastMessageReadIndex;
            
            // creating notification "for my self"

            // If we don't do this then the command will fail validation and result in an error
            if (unreadMessagesCount > 0)
            {
                var command = new CreateMessageUnreadNotificationCommand(
                    request.User,
                    unreadMessagesCount,
                    conversation.GetId()
                ); 
            
                await _mediator.Send(command, cancellationToken);
            }
            
            return model;
        }


    }
}

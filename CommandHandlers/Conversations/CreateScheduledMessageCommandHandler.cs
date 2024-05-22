using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Entities.Attachments;
using WildHealth.Common.Models.Conversations;
using System.Collections.Generic;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class
        CreateScheduledMessageCommandHandler : IRequestHandler<CreateScheduledMessageCommand, ScheduledMessageModel>
    {
        private readonly IScheduledMessagesService _scheduledMessagesService;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        
        public CreateScheduledMessageCommandHandler(
            IScheduledMessagesService scheduledMessagesService,
            ILogger<CreateScheduledMessageCommandHandler> logger,
            IMediator mediator)
        {
            _scheduledMessagesService = scheduledMessagesService;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<ScheduledMessageModel> Handle(CreateScheduledMessageCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                $"Creating conversation scheduled message for employee participant with id {request.ParticipantEmployeeId} has started.");

            var model = new ScheduledMessage(request.Message, request.TimeToSend, request.ConversationId,
                request.ParticipantEmployeeId);

            var scheduledMessage = await _scheduledMessagesService.CreateAsync(model);
            var scheduledMessageId = scheduledMessage.Id ?? 0;

            _logger.LogInformation(
                $"Creating conversation scheduled message for employee participant with id {request.ParticipantEmployeeId} has finished.");
            var uploadAttachments = new List<Attachment>();
            if (request.Attachments != null)
            {
                uploadAttachments =
                    await _mediator.Send(
                        new UploadScheduledMessageAttachmentsCommand(request.Attachments, scheduledMessageId),
                        cancellationToken);
            }

            var result = new ScheduledMessageModel
            {
                Id = scheduledMessageId,
                Message = scheduledMessage.Message,
                TimeToSend = scheduledMessage.TimeToSend,
                ConversationId = scheduledMessage.ConversationId,
                ParticipantId = scheduledMessage.ConversationParticipantEmployeeId,
                UploadedAttachments = uploadAttachments.ToArray()
            };

            return result;
        }
    }
}
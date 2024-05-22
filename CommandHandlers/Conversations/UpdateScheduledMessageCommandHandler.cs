using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.Conversations;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.Conversations;
using WildHealth.Domain.Entities.Attachments;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class UpdateScheduledMessageCommandHandler  : IRequestHandler<UpdateScheduledMessageCommand, ScheduledMessageModel>
{
    private const string Container = AzureBlobContainers.Attachments;
    private readonly IScheduledMessagesService _scheduledMessagesService;
    private readonly IAttachmentsService _attachmentsService;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IAzureBlobService _azureBlobService;
    
    public UpdateScheduledMessageCommandHandler(
        IScheduledMessagesService scheduledMessagesService,
        IAttachmentsService attachmentsService,
        ILogger<UpdateScheduledMessageCommandHandler> logger, 
        IMediator mediator, 
        IAzureBlobService azureBlobService)
    {
        _scheduledMessagesService = scheduledMessagesService;
        _attachmentsService = attachmentsService;
        _logger = logger;
        _mediator = mediator;
        _azureBlobService = azureBlobService;
    }

    public async Task<ScheduledMessageModel> Handle(UpdateScheduledMessageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Updating conversation scheduled message for employee participant with id {request.ParticipantEmployeeId} has started.");
        
        var model = new ScheduledMessage(request.ScheduledMessageId, request.Message, request.TimeToSend, request.ConversationId, request.ParticipantEmployeeId);
        
        var scheduledMessage = await _scheduledMessagesService.UpdateAsync(model);
        _logger.LogInformation($"Updating conversation scheduled message for employee participant with id {request.ParticipantEmployeeId} has finished.");
        
        var uploadedAttachments = new List<Attachment>();
        if (request.Attachments != null)
        {
            uploadedAttachments = await _mediator.Send(
                    new UploadScheduledMessageAttachmentsCommand(request.Attachments, request.ScheduledMessageId),
                    cancellationToken);
        }

        if (request.RemoveAttachments != null &&  request.RemoveAttachments.Length > 0)
        {
            _logger.LogInformation($"Attachments removing for scheduledMessage with id: {request.ScheduledMessageId} has started.");
            
            foreach (var attachmentToDeleteId in request.RemoveAttachments)
            {
                var attachmentToDelete = await _attachmentsService.GetByIdAsync(attachmentToDeleteId);
                await _attachmentsService.DeleteAttachmentAsync(attachmentToDeleteId);
                try
                {
                    await _azureBlobService.DeleteBlobAsync(Container, attachmentToDelete.Name);
                }
                catch (Exception ex)
                {
                    // If deleting from the blob fails for some reason, we still want to delete other attachments from the db
                    _logger.LogError(
                        $"Attachment with id: {attachmentToDeleteId} deletion from Azure blob for scheduledMessageId with id: {request.ScheduledMessageId} has failed. {ex.Message}");
                }
            }
            _logger.LogInformation($"Attachments removing for scheduledMessage with id: {request.ScheduledMessageId} has finished.");
        }

        var result = new ScheduledMessageModel() {
            Id = request.ScheduledMessageId,
            Message = scheduledMessage.Message,
            TimeToSend = scheduledMessage.TimeToSend,
            ConversationId = scheduledMessage.ConversationId,
            ParticipantId = scheduledMessage.ConversationParticipantEmployeeId,
            UploadedAttachments = uploadedAttachments
        };

        return result;
    }
}
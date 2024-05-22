using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Extensions.BlobFiles;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Common.Constants;
using WildHealth.Common.Extensions;
using WildHealth.Domain.Entities.Attachments;
using WildHealth.Domain.Enums.Attachments;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class UploadScheduledMessageAttachmentsCommandHandler : IRequestHandler<UploadScheduledMessageAttachmentsCommand, List<Attachment>>
{
    private const string Container = AzureBlobContainers.Attachments;
    private const AttachmentType AttachmentType = WildHealth.Domain.Enums.Attachments.AttachmentType.ScheduledMessageAttachment;
    private readonly IAttachmentsService _attachmentsService;
    private readonly IAzureBlobService _azureBlobService;
    private readonly ILogger _logger;

    public UploadScheduledMessageAttachmentsCommandHandler(
        IAttachmentsService attachmentsService, 
        IAzureBlobService azureBlobService, 
        ILogger<UploadScheduledMessageAttachmentsCommandHandler> logger)
    {
        _attachmentsService = attachmentsService;
        _azureBlobService = azureBlobService;
        _logger = logger;
    }

    public async Task<List<Attachment>> Handle(UploadScheduledMessageAttachmentsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Attachments uploading for scheduledMessage with id: {request.ScheduledMessageId} has started.");
        var uploadedAttachments = new List<Attachment>();
        foreach (var attachment in request.Attachments)
        {
            var bytes = await attachment.GetBytes();
            var fileName = attachment.GenerateStorageFileName(DateTime.Now);
            var path = await _azureBlobService.CreateUpdateBlobBytes(Container, fileName, bytes);

            var uploadedAttachment = await _attachmentsService.CreateOrUpdateWithBlobAsync(
                attachmentName: fileName,
                description: Container,
                attachmentType: AttachmentType,
                path: path,
                referenceId: request.ScheduledMessageId
            );
            
            uploadedAttachments.Add(uploadedAttachment);
        }
        _logger.LogInformation($"Attachments uploading for scheduledMessage with id: {request.ScheduledMessageId} has finished.");
        return uploadedAttachments;
    }
}
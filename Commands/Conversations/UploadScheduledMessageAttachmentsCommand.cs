using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Domain.Entities.Attachments;

namespace WildHealth.Application.Commands.Conversations;

public class UploadScheduledMessageAttachmentsCommand : IRequest<List<Attachment>>
{
    public UploadScheduledMessageAttachmentsCommand(IFormFile[] attachments, int scheduledMessageId)
    {
        Attachments = attachments;
        ScheduledMessageId = scheduledMessageId;
    }

    public IFormFile[] Attachments { get; }
    public int ScheduledMessageId { get;  }
    
}
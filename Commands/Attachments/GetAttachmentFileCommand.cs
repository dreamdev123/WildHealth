using MediatR;
using WildHealth.Domain.Entities.Attachments;

namespace WildHealth.Application.Commands.Attachments;

public class GetAttachmentFileCommand : IRequest<(Attachment? attachment, byte[] bytes)>
{
    public int Id { get; }
    
    public GetAttachmentFileCommand(int id)
    {
        Id = id;
    }
}
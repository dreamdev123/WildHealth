using MediatR;

namespace WildHealth.Application.Commands.Documents
{
    public class DeleteDocumentCommand : IRequest
    {
        public int AttachmentId { get; set; }
    }
}

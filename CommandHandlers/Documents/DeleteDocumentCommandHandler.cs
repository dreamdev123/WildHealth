using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Documents;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Common.Constants;

namespace WildHealth.Application.CommandHandlers.Documents
{
    public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
    {
        private readonly IAttachmentsService _attachmentsService;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IPermissionsGuard _permissionGuard;
        private const string Container = AzureBlobContainers.Attachments;

        public DeleteDocumentCommandHandler(
            IAttachmentsService attachmentsService,
            IAzureBlobService azureBlobService,
            IPermissionsGuard permissionGuard)
        {
            _attachmentsService = attachmentsService;
            _azureBlobService = azureBlobService;
            _permissionGuard = permissionGuard;
        }

        public async Task Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            var attachment = await _attachmentsService.GetByIdAsync(request.AttachmentId);

            _permissionGuard.AssertLocationIdPermissions(attachment.UserAttachment.User.Patient.LocationId);

            await _attachmentsService.DeleteAsync(attachment);

            await _azureBlobService.DeleteBlobAsync(Container, attachment.Name);
        }
    }
}

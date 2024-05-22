using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Services.Attachments;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.CommandHandlers.Employees;

public class GetEmployeePhotoUrlCommandHandler : IRequestHandler<GetEmployeePhotoUrlCommand, string?>
{
    public IAttachmentsService _attachmentsService;

    public GetEmployeePhotoUrlCommandHandler(IAttachmentsService attachmentsService)
    {
        _attachmentsService = attachmentsService;
    }

    public async Task<string?> Handle(GetEmployeePhotoUrlCommand command, CancellationToken cancellationToken)
    {
        var attachment = await _attachmentsService.GetUserAttachmentByTypeAsync(command.UserId, AttachmentType.ProfilePhoto);

        if (attachment is not null)
        {
            var photoUrlTry = await _attachmentsService.GetSecuredUrl(attachment.GetId(), 5).ToTry();
            
            return photoUrlTry.ValueOr(string.Empty);
        }

        return string.Empty;
    }
}

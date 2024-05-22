using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Attachments;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Application.Extensions.BlobFiles;

namespace WildHealth.Application.CommandHandlers.Employees;

public class UpdateProfilePhotoCommandHandler : IRequestHandler<UpdateProfilePhotoCommand>
{
    private const string Container = AzureBlobContainers.Attachments;
    private const AttachmentType AttachmentType = WildHealth.Domain.Enums.Attachments.AttachmentType.ProfilePhoto;
    
    private readonly IAttachmentsService _attachmentsService;
    private readonly IAzureBlobService _azureBlobService;

    public UpdateProfilePhotoCommandHandler(
        IAttachmentsService attachmentsService, 
        IAzureBlobService azureBlobService)
    {
        _attachmentsService = attachmentsService;
        _azureBlobService = azureBlobService;
    }

    public async Task Handle(UpdateProfilePhotoCommand request, CancellationToken cancellationToken)
    {
        var userId = request.Employee.User.GetId();
        
        var bytes = await FormatAndCovertPhotoAsync(request.File);

        var fileName = request.File.GenerateStorageFileName(request.Employee.User.GetId(), AttachmentType.ProfilePhoto);

        var filePath = await CreateBlobAsync(fileName, bytes);

        await ClearOldPhoto(userId);

        await CreateNewAttachment(request.Employee.User, fileName, filePath);
    }

    private async Task ClearOldPhoto(int userId)
    {
        var attachment = await _attachmentsService.GetUserAttachmentByTypeAsync(userId, AttachmentType);
        if (attachment is not null)
        {
            await _attachmentsService.DeleteAsync(attachment);
        }
    }

    private async Task CreateNewAttachment(User user, string fileName, string filePath)
    {
        var attachment = new UserAttachment(
            type: AttachmentType,
            name: fileName,
            description: $"File from user with id {user.GetId()} in category {AttachmentType.ToString()}",
            path: filePath,
            user: user);
        
        await _attachmentsService.CreateAsync(attachment);
    }
    
    private async Task<byte[]> FormatAndCovertPhotoAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new AppException(HttpStatusCode.BadRequest, "File is empty.");
        }

        using var originImageStream = new MemoryStream();
        await file.CopyToAsync(originImageStream);
        
        return originImageStream.ToArray();
    }

    private async Task<string> CreateBlobAsync(string fileName, Byte[] bytes)
    {
        return await _azureBlobService.CreateUpdateBlobBytes(Container, fileName, bytes);
    }
}
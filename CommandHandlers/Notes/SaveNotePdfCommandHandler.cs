using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Extensions.BlobFiles;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.Notes;
using WildHealth.Common.Constants;
using WildHealth.Domain.Exceptions;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Notes;

public class SaveNotePdfCommandHandler : IRequestHandler<SaveNotePdfCommand>
{
    private readonly IAzureBlobService _azureBlobService;
    private readonly INoteService _noteService;
    private const string Container = AzureBlobContainers.Attachments;
    private readonly MaterializeFlow _materialize;

    public SaveNotePdfCommandHandler(
        IAzureBlobService azureBlobService,
        INoteService noteService,
         MaterializeFlow materialize)
    {
        _azureBlobService = azureBlobService;
        _noteService = noteService;
        _materialize = materialize;
    }

    public async Task Handle(SaveNotePdfCommand request, CancellationToken cancellationToken)
    {
        ValidateExtension(request.NotePdf);
        var note = await _noteService.GetByIdAsync(request.NoteId);
        
        if (request.WithAmendments && note.NotePdfAttachmentWithAmendments != null)
        {
            throw new AppException(HttpStatusCode.Conflict,"PDF file for the note has already been uploaded");
        }
        
        if (!request.WithAmendments && note.NotePdfAttachment != null)
        {
            throw new AppException(HttpStatusCode.Conflict,"PDF file for the note has already been uploaded");
        }

        
        var bytes = await request.NotePdf.GetBytes();
        var fileName = request.NotePdf.GenerateStorageFileName(request.NoteId, request.WithAmendments);
        
        var blobUri = await _azureBlobService.CreateUpdateBlobBytes(
            containerName: Container,
            blobName: fileName,
            fileBytes: bytes
        );

        await new SaveNotePdfFlow(request.WithAmendments, fileName, blobUri, request.NoteId).Materialize(_materialize);
    }

    private void ValidateExtension(IFormFile requestNotePdf)
    {
        var ext = requestNotePdf.ContentType.ToLower();
        if (ext != "application/pdf")
        {
            throw new DomainException("The file type must be pdf.");
        }
    }
}
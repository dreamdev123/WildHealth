using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.Notes;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Attachments;

namespace WildHealth.Application.CommandHandlers.Notes;

public class SaveFilesForNotesCommandHandler: IRequestHandler<SaveFilesForNotesCommand>
{
    private readonly INoteService _noteService;
    private readonly IAttachmentsService _attachmentsService;
    private readonly IAzureBlobService _azureBlobService;
    private readonly MaterializeFlow _materializeFlow;
    
    public SaveFilesForNotesCommandHandler(
        INoteService noteService,
        IAttachmentsService attachmentsService,
        IAzureBlobService azureBlobService, 
        MaterializeFlow materializeFlow)
    {
        _noteService = noteService;
        _attachmentsService = attachmentsService;
        _azureBlobService = azureBlobService;
        _materializeFlow = materializeFlow;
    }

    public async Task Handle(SaveFilesForNotesCommand request, CancellationToken cancellationToken)
    {
        var alreadyTransferred = new List<string>();

        var firstAttachment = request.AttachmentsForNotes.FirstOrDefault();
        
        bool isParsedNoteId = int.TryParse(firstAttachment?.NoteId, out int noteId);

        if (!isParsedNoteId)
        {
            return;
        }
        
        if (request.AttachmentsForNotes.Count > 0)
        {
            var note = await _noteService.GetByIdAsync(noteId);

            if (note.OriginalNoteId != null)
            {
                var originalNote = await _noteService.GetByIdAsync(note.OriginalNoteId.Value);

                var flow = new AttachOldFilesToNewNoteFlow(note, originalNote, request.AttachmentsForNotes);

                var result = (await flow.Materialize(_materializeFlow)) as AttachOldFilesToNewNoteFlowResult;
                
                alreadyTransferred = result!.AlreadyTransferred;
            }
        }
        
        var resultAttachments = request.AttachmentsForNotes.Where(x => !alreadyTransferred.Contains(x.AttachmentId)).ToArray();
        
        var noteContent = await _noteService.GetContentByNoteIdAsync(noteId);
        
        // Upload files
        foreach (var attachment in resultAttachments.Where(x => !x.ForDelete ?? true))
        {
            if (!string.IsNullOrEmpty(attachment.AttachmentId))
            {
                continue;
            }

            var bytes = await GetBytesAsync(attachment.Attachment);
            
            var fileName = _noteService.GenerateFileName(noteContent.GetId(), attachment.Attachment.FileName, bytes.Length.ToString());
                
            var blobUri = await _azureBlobService.CreateUpdateBlobBytes(
                containerName: AzureBlobContainers.Attachments,
                blobName: fileName,
                fileBytes: bytes
            );

            if (string.IsNullOrEmpty(blobUri))
            {
                continue;
            }

            var noteContentAttachment = new NoteContentAttachment(
                AttachmentType.NoteAttachment, 
                fileName, 
                $"Attachment added to note with content id {noteContent.GetId()}", 
                blobUri, 
                noteContent);

            await _attachmentsService.CreateAsync(noteContentAttachment);
        }

        // Deleted previously uploaded files
        foreach (var attachment in resultAttachments.Where(x => x.ForDelete.HasValue && x.ForDelete.Value))
        {
            if (string.IsNullOrEmpty(attachment.AttachmentId))
            {
                continue;
            }

            bool isParsed = int.TryParse(attachment.AttachmentId, out int attachmentId);

            if (!isParsed)
            {
                continue;
            }
            
            var attachmentForDelete = await _attachmentsService.GetByIdAsync(attachmentId);

            await _attachmentsService.DeleteAsync(attachmentForDelete);

            await _azureBlobService.DeleteBlobAsync(AzureBlobContainers.Attachments, attachmentForDelete.Name);
        }
    }
    
    private static async Task<byte[]> GetBytesAsync(IFormFile file)
    {
        await using var stream = new MemoryStream();

        await file.CopyToAsync(stream);

        return stream.ToArray();
    }
}
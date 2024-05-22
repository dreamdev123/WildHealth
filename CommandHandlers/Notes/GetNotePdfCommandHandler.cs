using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.Notes;
using WildHealth.Domain.Entities.Attachments;

namespace WildHealth.Application.CommandHandlers.Notes;

public class GetNotePdfCommandHandler : IRequestHandler<GetNotePdfCommand, (Attachment? attachment, byte[] bytes)>
{
    private readonly IAttachmentsService _attachmentsService;
    private readonly INoteService _noteService;

    public GetNotePdfCommandHandler(
        IAttachmentsService attachmentsService, 
        INoteService noteService)
    {
        _attachmentsService = attachmentsService;
        _noteService = noteService;
    }

    public async Task<(Attachment? attachment, byte[] bytes)> Handle(GetNotePdfCommand request, CancellationToken cancellationToken)
    {
        var note = await _noteService.GetByIdAsync(request.NoteId);
        var notePdf = request.WithAmendments
            ? note.NotePdfAttachmentWithAmendments?.Attachment
            : note.NotePdfAttachment?.Attachment;

        if (notePdf is null)
            return (
                attachment: null,
                bytes: Array.Empty<byte>()
            );
        
        var bytes = await _attachmentsService.GetFileByPathAsync(notePdf.Path);
        return (notePdf, bytes);
    }
}
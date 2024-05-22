using System.Collections.Generic;
using MediatR;
using WildHealth.Common.Models.Notes;

namespace WildHealth.Application.Commands.Notes;

public class SaveFilesForNotesCommand : IRequest
{
    public ICollection<AttachmentsForNote> AttachmentsForNotes { get; set; }

    public SaveFilesForNotesCommand(ICollection<AttachmentsForNote> attachmentsForNotes)
    {
        AttachmentsForNotes = attachmentsForNotes;
    }
}
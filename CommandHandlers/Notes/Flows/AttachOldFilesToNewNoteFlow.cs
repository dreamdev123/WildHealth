using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Attachments;

namespace WildHealth.Application.CommandHandlers.Notes.Flows;

public class AttachOldFilesToNewNoteFlow : IMaterialisableFlow
{
    private readonly Note _note;
    private readonly Note _originalNote;
    private readonly ICollection<AttachmentsForNote> _attachmentsForNotes;

    public AttachOldFilesToNewNoteFlow(Note note, Note originalNote, ICollection<AttachmentsForNote> attachmentsForNotes)
    {
        _note = note;
        _originalNote = originalNote;
        _attachmentsForNotes = attachmentsForNotes;
    }

    public MaterialisableFlowResult Execute()
    {
        var alreadyTransferred = new List<string>();
        var noteContentAttachments = new List<NoteContentAttachment>();
        
        if (_attachmentsForNotes.Count > 0)
        {
            if (_originalNote != null)
            {
                foreach (var attachment in _originalNote.Content.Attachments)
                {
                    var attachmentModel = _attachmentsForNotes
                        .FirstOrDefault(x =>
                            x.AttachmentId.Equals(attachment.AttachmentId.ToString())
                        );
                    
                    // skip if current attachment was for removed (we don`t need to transfer it for new note)
                    if (attachmentModel != null && attachmentModel.ForDelete.HasValue && attachmentModel.ForDelete.Value)
                    {
                        alreadyTransferred.Add(attachment.AttachmentId.ToString());
                        continue;
                    }
                    
                    var noteContentAttachment = new NoteContentAttachment(
                        AttachmentType.NoteAttachment, 
                        attachment.Attachment.Name, 
                        attachment.Attachment.Description, 
                        attachment.Attachment.Path, 
                        _note.Content);
                        
                    noteContentAttachments.Add(noteContentAttachment);
                    alreadyTransferred.Add(attachment.AttachmentId.ToString());
                }
            }
        }

        return new AttachOldFilesToNewNoteFlowResult(noteContentAttachments.Select(x => x.Added()).ToList(), alreadyTransferred);
    }
}


public record AttachOldFilesToNewNoteFlowResult(List<EntityAction> Attachments, List<string> AlreadyTransferred) : MaterialisableFlowResult(Attachments);
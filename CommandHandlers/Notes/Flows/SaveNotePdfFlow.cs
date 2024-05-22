using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Attachments;

namespace WildHealth.Application.CommandHandlers.Notes.Flows;

public class SaveNotePdfFlow : IMaterialisableFlow
{
    private readonly bool _withAmendments;
    private readonly string _fileName;
    private readonly string _blobUri;
    private readonly int _noteId;

    public SaveNotePdfFlow(bool withAmendments, string fileName, string blobUri, int noteId)
    {
        _withAmendments = withAmendments;
        _fileName = fileName;
        _blobUri = blobUri;
        _noteId = noteId;
    }

    public MaterialisableFlowResult Execute()
    {
        return _withAmendments 
            ? SaveNotePdfWithAmendments() 
            : SaveNotePdfWithoutAmendments();
    }
    
    private MaterialisableFlowResult SaveNotePdfWithoutAmendments()
    {
        var attachment = new NotePdfAttachment(
            noteId: _noteId,
            type: AttachmentType.NotePdf,
            name: _fileName,
            description: AzureBlobContainers.Attachments,
            path: _blobUri);

        return attachment.Added();
    }
    
    private MaterialisableFlowResult SaveNotePdfWithAmendments()
    {
        var attachment = new NotePdfAttachmentWithAmendments(
            noteId: _noteId,
            type: AttachmentType.NotePdfWithAmendments,
            name: _fileName,
            description: AzureBlobContainers.Attachments,
            path: _blobUri);

        return attachment.Added();
    }
}
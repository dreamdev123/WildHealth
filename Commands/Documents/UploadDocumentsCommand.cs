using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using WildHealth.Domain.Entities.Attachments;
using WildHealth.Domain.Enums.Attachments;
using MediatR;
namespace WildHealth.Application.Commands.Documents;

public class UploadDocumentsCommand : IRequest<List<Attachment>>
{
    public IFormFile[] Documents { get; }
    public AttachmentType AttachmentType { get; }
    public int PatientId { get; }
    public int UploadedByUserId { get; }
    public bool IsSendToKb { get; }
    public int[]? IsViewableByPatientFileIndexes { get; }
        
    public UploadDocumentsCommand(IFormFile[] documents, AttachmentType attachmentType, int patientId, int uploadedByUserId, bool isSendToKb, int[]? isViewableByPatientFileIndexes = default)
    {
        Documents = documents;
        AttachmentType = attachmentType;
        PatientId = patientId;
        UploadedByUserId = uploadedByUserId;
        IsSendToKb = isSendToKb;
        IsViewableByPatientFileIndexes = isViewableByPatientFileIndexes;
    }
}
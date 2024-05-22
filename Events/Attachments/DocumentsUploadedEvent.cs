using MediatR;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Events.Attachments;

public class DocumentsUploadedEvent : INotification
{
    public Patient Patient { get; }
    public int Amount { get; }
    public int UploadedByUserId { get; }
    
    public DocumentsUploadedEvent(Patient patient, int amount, int uploadedByUserId)
    {
        Patient = patient;
        Amount = amount;
        UploadedByUserId = uploadedByUserId;
    }
}
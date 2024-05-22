using MediatR;
using WildHealth.Domain.Enums.Inputs;

namespace WildHealth.Application.Events.Inputs
{
    public class FileInputsUploadedEvent : INotification
    {
        public int PatientId { get; }
        
        public FileInputType InputType { get; }

        public string? OrderNumber { get; }
        public string FilePath { get; }

        public FileInputsUploadedEvent(
            int patientId, 
            FileInputType inputType,
            string filePath,
            string? orderNumber = null)
        {
            PatientId = patientId;
            InputType = inputType;
            FilePath = filePath;
            OrderNumber = orderNumber;
        }
    }
}
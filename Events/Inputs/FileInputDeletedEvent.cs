using MediatR;
using WildHealth.Domain.Enums.Inputs;

namespace WildHealth.Application.Events.Inputs
{
    public class FileInputDeletedEvent : INotification
    {
        public int PatientId { get; }
        
        public FileInputType InputType { get; }

        public FileInputDeletedEvent(
            int patientId, 
            FileInputType inputType)
        {
            PatientId = patientId;
            InputType = inputType;
        }
    }
}
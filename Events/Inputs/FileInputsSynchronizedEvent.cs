using WildHealth.Domain.Enums.Inputs;
using MediatR;

namespace WildHealth.Application.Events.Inputs
{
    public class FileInputsSynchronizedEvent : INotification
    {
        public int PatientId { get; }
        public FileInputType Type { get; }
        public string FilePath { get; }


        public FileInputsSynchronizedEvent(int patientId, FileInputType type, string filePath)
        {
            PatientId = patientId;
            Type = type;
            FilePath = filePath;
        }
    }
}
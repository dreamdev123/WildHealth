using MediatR;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Commands.Patients
{
    /// <summary>
    /// Represents command for patient creating
    /// </summary>
    public class ChangePracticeCopyCommand : IRequest<Patient>
    {
        public int PatientId { get; protected set; }
        public int ToPracticeId { get; protected set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="toPracticeId"></param>
        public ChangePracticeCopyCommand(
            int patientId, 
            int toPracticeId)
        {
            PatientId = patientId;
            ToPracticeId = toPracticeId;
        }
    }
}
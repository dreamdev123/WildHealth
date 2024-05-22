using MediatR;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Patient;

namespace WildHealth.Application.Commands.Patients
{
    public class UpdatePatientStatusesCommand : IRequest<Patient>
    {
        public int Id { get; }

        public PatientJourneyStatus JourneyStatus { get; }

        public PatientDnaStatus DnaStatus { get; }

        public PatientLabsStatus LabsStatus { get; }

        public PatientEpigeneticStatus EpigeneticStatus { get;  }

        public UpdatePatientStatusesCommand(
            int id,
            PatientJourneyStatus journeyStatus,
            PatientDnaStatus dnaStatus,
            PatientLabsStatus labsStatus,
            PatientEpigeneticStatus epigeneticStatus)
        {
            Id = id;
            JourneyStatus = journeyStatus;
            DnaStatus = dnaStatus;
            LabsStatus = labsStatus;
            EpigeneticStatus = epigeneticStatus;
        }
    }
}

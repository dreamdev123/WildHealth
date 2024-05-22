using MediatR;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Commands.Patients
{
    public class TransferPatientToLocationCommand : IRequest<Patient>
    {
        public int PatientId { get; }

        public int LocationId { get; }

        public TransferPatientToLocationCommand(
            int patientId,
            int locationId)
        {
            PatientId = patientId;
            LocationId = locationId;
        }
    }
}

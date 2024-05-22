using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class UpdatePatientStatusesCommandHandler : IRequestHandler<UpdatePatientStatusesCommand, Patient>
    {
        private readonly IPatientsService _patientsService;
        private readonly IPermissionsGuard _permissionsGuard;

        public UpdatePatientStatusesCommandHandler(IPatientsService patientsService, IPermissionsGuard permissionsGuard)
        {
            _patientsService = patientsService;
            _permissionsGuard = permissionsGuard;
        }

        public async Task<Patient> Handle(UpdatePatientStatusesCommand command, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(command.Id);

            _permissionsGuard.AssertPermissions(patient);

            patient.JourneyStatus = command.JourneyStatus;
            patient.DnaStatus = command.DnaStatus;
            patient.LabsStatus = command.LabsStatus;
            patient.EpigeneticStatus = command.EpigeneticStatus;

            await _patientsService.UpdateAsync(patient);

            return patient;
        }
    }
}

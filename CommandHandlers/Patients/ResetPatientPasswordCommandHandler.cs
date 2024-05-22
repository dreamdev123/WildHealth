using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class ResetPatientPasswordCommandHandler: IRequestHandler<ResetPatientPasswordCommand>
    {
        private readonly IGeneralRepository<Patient> _patientRepository;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IAuthService _authService;

        public ResetPatientPasswordCommandHandler(
            IGeneralRepository<Patient> patientRepository,
            IPermissionsGuard permissionsGuard,
            IAuthService authService)
        {
            _patientRepository = patientRepository;
            _permissionsGuard = permissionsGuard;
            _authService = authService;
        }

        public async Task Handle(ResetPatientPasswordCommand command, CancellationToken cancellationToken) 
        {
            var patient = await FetchPatientAsync(command.PatientId);

            if (command.AssertPermissions)
            {
                _permissionsGuard.AssertPermissions(patient);
            }

            await _authService.UpdatePassword(patient.User.Identity, command.NewPassword);
        }

        #region private

        private async Task<Patient> FetchPatientAsync(int patientId)
        {
            var patient = await _patientRepository
                .All()
                .IncludeIdentity()
                .FirstOrDefaultAsync(x => x.Id == patientId);

            if (patient == null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Patient not found", exceptionParam);
            }

            return patient;
        }

        #endregion
    }
}

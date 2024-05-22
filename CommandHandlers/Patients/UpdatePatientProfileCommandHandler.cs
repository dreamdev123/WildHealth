using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Utils.PermissionsGuard;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class UpdatePatientProfileCommandHandler : IRequestHandler<UpdatePatientProfileCommand, Patient>
    {
        private readonly IPatientsService _patientsService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly ITransactionManager _transactionManager;
        private readonly IMediator _mediator;

        public UpdatePatientProfileCommandHandler(
            IPatientsService patientsService, 
            IPermissionsGuard permissionsGuard,
            ITransactionManager transactionManager,
            IMediator mediator)
        {
            _patientsService = patientsService;
            _permissionsGuard = permissionsGuard;
            _transactionManager = transactionManager;
            _mediator = mediator;
        }

        public async Task<Patient> Handle(UpdatePatientProfileCommand command, CancellationToken cancellationToken)
        {
            var patient = await GetPatientAsync(command);

            var updateUserCommand = new UpdateUserCommand(
                id: patient.User.GetId(),
                firstName: command.FirstName,
                lastName: command.LastName,
                birthday: command.Birthday,
                gender: command.Gender,
                email: command.Email,
                phoneNumber: command.PhoneNumber,
                billingAddress: command.BillingAddress,
                shippingAddress: command.ShippingAddress,
                userType: null
            );

            await using var transaction = _transactionManager.BeginTransaction();
            try
            {
                await _mediator.Send(updateUserCommand, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            var @event = new PatientUpdatedEvent(patient.GetId(), Enumerable.Empty<int>());
            
            await _mediator.Publish(@event, cancellationToken);

            return patient;
        }
        
        #region private

        /// <summary>
        /// Fetches and returns patient depends on identifier
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private async Task<Patient> GetPatientAsync(UpdatePatientProfileCommand command)
        {
            if (command.Id.HasValue)
            {
                var patient = await _patientsService.GetByIdAsync(command.Id.Value);

                _permissionsGuard.AssertPermissions(patient);

                return patient;
            }

            if (command.IntakeId.HasValue)
            {
                return await _patientsService.GetByIntakeIdAsync(command.IntakeId.Value);
            }

            throw new ArgumentException();
        }
        
        #endregion
    }
}
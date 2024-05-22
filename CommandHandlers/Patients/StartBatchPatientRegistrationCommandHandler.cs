using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Shared.Data.Managers.TransactionManager;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class StartBatchPatientRegistrationCommandHandler : IRequestHandler<StartBatchPatientRegistrationCommand, IEnumerable<Patient>>
    {
        private readonly IMediator _mediator;
        private readonly ITransactionManager _transactionManager;

        public StartBatchPatientRegistrationCommandHandler(
            IMediator mediator,
            ITransactionManager transactionManager)
        {
            _mediator = mediator;
            _transactionManager = transactionManager;
        }

        public async Task<IEnumerable<Patient>> Handle(StartBatchPatientRegistrationCommand command, CancellationToken cancellationToken)
        {
            var patients = new List<Patient>();

            await using var transaction = _transactionManager.BeginTransaction();

            try
            {
                foreach (var registrationModel in command.Patients)
                {
                    var startPatientRegistrationCommand = new StartPatientRegistrationCommand(
                        firstName: registrationModel.FirstName.Trim(),
                        lastName: registrationModel.LastName.Trim(),
                        email: registrationModel.Email.Trim(),
                        birthday: registrationModel.Birthday,
                        phoneNumber: registrationModel.PhoneNumber?.Trim(),
                        practiceId: registrationModel.PracticeId,
                        fellowId: registrationModel.FellowId,
                        gender: registrationModel.Gender
                    );

                    var patient = await _mediator.Send(startPatientRegistrationCommand, cancellationToken);
                    patients.Add(patient);
                }

                await transaction.CommitAsync(cancellationToken);

            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }

            return patients;
        }
    }
}

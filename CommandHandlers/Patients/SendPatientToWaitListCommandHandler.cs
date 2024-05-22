using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Shared.Data.Repository;
using WildHealth.Domain.Entities.Patients;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class SendPatientToWaitListCommandHandler : IRequestHandler<SendPatientToWaitListCommand>
    {
        private readonly IGeneralRepository<PatientsWaitList> _waitListRepository;

        public SendPatientToWaitListCommandHandler(IGeneralRepository<PatientsWaitList> waitListRepository)
        {
            _waitListRepository = waitListRepository;
        }

        public async Task Handle(SendPatientToWaitListCommand command, CancellationToken cancellationToken)
        {
            var waitList = new PatientsWaitList
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                PhoneNumber = command.PhoneNumber,
                Email = command.Email,
                State = command.State,
                PaymentPlanId = command.PaymentPlanId,
                PracticeId = command.PracticeId
            };

            await _waitListRepository.AddAsync(waitList);
            await _waitListRepository.SaveAsync();
        }
    }
}
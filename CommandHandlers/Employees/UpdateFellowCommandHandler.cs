using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Employees;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Application.CommandHandlers.Employees.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Fellows;

namespace WildHealth.Application.CommandHandlers.Employees
{
    public class UpdateFellowCommandHandler : IRequestHandler<UpdateFellowCommand, Fellow>
    {
        private readonly IFellowsService _fellowsService;
        private readonly MaterializeFlow _materialization;

        public UpdateFellowCommandHandler(
            IFellowsService fellowsService,
            MaterializeFlow materialization)
        {
            _fellowsService = fellowsService;
            _materialization = materialization;
        }
        
        public async Task<Fellow> Handle(UpdateFellowCommand command, CancellationToken cancellationToken)
        {
            var fellow = await _fellowsService.GetByIdAsync(command.Id);

            var flow = new UpdateFellowFlow(
                fellow: fellow,
                firstName: command.FirstName,
                lastName: command.LastName,
                email: command.Email,
                phoneNumber: command.PhoneNumber,
                credentials: command.Credentials
            );

            await flow.Materialize(_materialization);

            return fellow;
        }
    }
}
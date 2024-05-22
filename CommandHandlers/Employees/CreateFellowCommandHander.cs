using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Employees;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Application.Services.Rosters;
using WildHealth.Application.CommandHandlers.Employees.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Employees
{
    public class CreateFellowCommandHandler : IRequestHandler<CreateFellowCommand, Fellow>
    {
        private readonly MaterializeFlow _materialization;
        private readonly IRostersService _rostersService;

        public CreateFellowCommandHandler(
            MaterializeFlow materialization,
            IRostersService rostersService)
        {
            _materialization = materialization;
            _rostersService = rostersService;
        }

        public async Task<Fellow> Handle(CreateFellowCommand command, CancellationToken cancellationToken)
        {
            var roster = await _rostersService.GetAsync(command.RosterId);

            var flow = new CreateFellowFlow(
                roster: roster,
                firstName: command.FirstName,
                lastName: command.LastName,
                email: command.Email,
                phoneNumber: command.PhoneNumber,
                credentials: command.Credentials
            );
            
            var fellow = await flow
                .Materialize(_materialization)
                .Select<Fellow>();
            
            return fellow;
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Rosters;
using WildHealth.Application.Services.Rosters;
using WildHealth.Domain.Entities.Employees;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Rosters;

public class CreateRosterCommandHandler : IRequestHandler<CreateRosterCommand, Roster>
{
    private readonly IRostersService _rostersService;

    public CreateRosterCommandHandler(IRostersService rostersService)
    {
        _rostersService = rostersService;
    }

    public async Task<Roster> Handle(CreateRosterCommand command, CancellationToken cancellationToken)
    {
        var roster = new Roster(command.Name);

        await _rostersService.CreateAsync(roster);

        return roster;
    }
}
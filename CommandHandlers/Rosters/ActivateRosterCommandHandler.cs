using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Rosters;
using WildHealth.Application.Services.Rosters;
using WildHealth.Domain.Entities.Employees;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Rosters;

public class ActivateRosterCommandHandler : IRequestHandler<ActivateRosterCommand, Roster>
{
    private readonly IRostersService _rostersService;

    public ActivateRosterCommandHandler(IRostersService rostersService)
    {
        _rostersService = rostersService;
    }

    public async Task<Roster> Handle(ActivateRosterCommand command, CancellationToken cancellationToken)
    {
        var roster = await _rostersService.GetAsync(command.Id);

        if (roster.IsActive == command.IsActive)
        {
            return roster;
        }

        roster.IsActive = command.IsActive;

        await _rostersService.UpdateAsync(roster);

        return roster;
    }
}
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using MediatR;
using WildHealth.Application.CommandHandlers.Auth.Flows;

namespace WildHealth.Application.CommandHandlers.Auth;

public class CheckIfEmailInUseCommandHandler : IRequestHandler<CheckIfEmailInUseCommand, bool>
{
    private readonly IAuthService _authService;

    public CheckIfEmailInUseCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<bool> Handle(CheckIfEmailInUseCommand command, CancellationToken cancellationToken)
    {
        if (await _authService.CheckIfEmailExistsAsync(command.Email) == false)
        {
            return false;
        }

        var identity = await _authService.GetByEmailAsync(command.Email);

        var flow = new CheckIfEmailInUseCommandFlow(identity);

        return flow.Execute().InUse;
    }
}
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Domain.PreauthorizeRequests.Services;
using WildHealth.Application.Services.Auth;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.CommandHandlers.Auth;

public class SetupPreauthorizePasswordCommandHandler : IRequestHandler<SetupPreauthorizePasswordCommand, User>
{
    private readonly IAuthService _authService;
    private readonly IPreauthorizeRequestsService _preauthorizeRequestsService;

    public SetupPreauthorizePasswordCommandHandler(
        IAuthService authService, 
        IPreauthorizeRequestsService preauthorizeRequestsService)
    {
        _authService = authService;
        _preauthorizeRequestsService = preauthorizeRequestsService;
    }

    public async Task<User> Handle(SetupPreauthorizePasswordCommand command, CancellationToken cancellationToken)
    {
        var request = await VerifyPreauthorizeRequestAsync(command.PreauthorizeRequestToken);
        
        await _authService.UpdatePassword(request.UserId, command.Password);
        
        return request.User;
    }
    
    #region private

    private async Task<PreauthorizeRequest> VerifyPreauthorizeRequestAsync(string preauthorizeRequestToken)
    {
        var request = await _preauthorizeRequestsService.GetByTokenAsync(preauthorizeRequestToken);

        if (request.IsCompleted || request.User.IsRegistrationCompleted)
        {
            throw new DomainException("Can't perform this operation because registration is already completed");
        }

        return request;
    }
    
    #endregion
}
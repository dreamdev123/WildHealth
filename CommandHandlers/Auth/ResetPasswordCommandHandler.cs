using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Shared.Utils.AuthTicket;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Auth
{
    /// <summary>
    /// Provides verify reset password command handler
    /// </summary>
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
    {
        private readonly IAuthService _authService;
        private readonly IAuthTicket _authTicket;

        public ResetPasswordCommandHandler(
            IAuthService authService,
            IAuthTicket authTicket)
        {
            _authService = authService;
            _authTicket = authTicket;
        }

        /// <summary>
        /// Handles command and resets user password
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            var identity = await _authService.GetByIdAsync(_authTicket.GetId());
            
            await _authService.UpdatePassword(identity, command.NewPassword);
            
            return true;
        }
    }
}
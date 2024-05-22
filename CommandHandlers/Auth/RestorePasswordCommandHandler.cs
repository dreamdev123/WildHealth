using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.ConfirmCodes;
using WildHealth.Shared.Data.Managers.TransactionManager;
using MediatR;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.CommandHandlers.Auth
{
    public class RestorePasswordCommandHandler : IRequestHandler<RestorePasswordCommand,User>
    {
        private readonly IAuthService _authService;
        private readonly IConfirmCodeService _confirmCodeService;
        private readonly ITransactionManager _transactionManager;

        public RestorePasswordCommandHandler(
            IAuthService authService, 
            IConfirmCodeService confirmCodeService, 
            ITransactionManager transactionManager)
        {
            _authService = authService;
            _confirmCodeService = confirmCodeService;
            _transactionManager = transactionManager;
        }

        public async Task<User> Handle(RestorePasswordCommand command, CancellationToken cancellationToken)
        {
            await using var transaction = _transactionManager.BeginTransaction();
            try
            {
                var code = await _confirmCodeService.ConfirmAsync(command.Code, command.ConfirmCodeType);
                
                await _authService.UpdatePassword(code.User.GetId(), command.Password);

                await transaction.CommitAsync(cancellationToken);

                return code.User;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
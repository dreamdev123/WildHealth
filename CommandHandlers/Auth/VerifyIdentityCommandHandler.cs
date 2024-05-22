using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.ConfirmCodes;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Data.Managers.TransactionManager;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Auth
{
    /// <summary>
    /// Provides verify identity command handler
    /// </summary>
    public class VerifyIdentityCommandHandler : IRequestHandler<VerifyIdentityCommand, bool>
    {
        private readonly IAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly IConfirmCodeService _confirmCodeService;
        private readonly ITransactionManager _transactionManager;

        public VerifyIdentityCommandHandler(
            IAuthService authService,
            IUsersService usersService, 
            IConfirmCodeService confirmCodeService, 
            ITransactionManager transactionManager)
        {
            _authService = authService;
            _usersService = usersService;
            _confirmCodeService = confirmCodeService;
            _transactionManager = transactionManager;
        }

        /// <summary>
        /// Handles command and verify identity
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> Handle(VerifyIdentityCommand command, CancellationToken cancellationToken)
        {
            var identity = await _authService.GetByIdAsync(command.UserId);
            
            var user = await _usersService.GetAsync(command.UserId);
            
            await using var transaction = _transactionManager.BeginTransaction();
            try
            {
                await _confirmCodeService.ConfirmAsync(user, command.Code, ConfirmCodeType.ActivateIdentity);

                await _authService.VerifyAsync(identity);

                await transaction.CommitAsync(cancellationToken);
                
                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
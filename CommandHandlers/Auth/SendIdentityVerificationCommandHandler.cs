using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Communication;
using WildHealth.Application.Services.ConfirmCodes;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Auth
{
    /// <summary>
    /// Provides Send identity verification Command handler
    /// </summary>
    public class SendIdentityVerificationCommandHandler : IRequestHandler<SendIdentityVerificationCommand, bool>
    {
        private readonly IAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly IConfirmCodeService _confirmCodeService;
        private readonly ICommunicationService _communicationService;

        public SendIdentityVerificationCommandHandler(
            IAuthService authService,
            IUsersService usersService,
            IConfirmCodeService confirmCodeService,
            ICommunicationService communicationService)
        {
            _authService = authService;
            _usersService = usersService;
            _confirmCodeService = confirmCodeService;
            _communicationService = communicationService;
        }

        /// <summary>
        /// Handles command and sends identity verification
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> Handle(SendIdentityVerificationCommand command, CancellationToken cancellationToken)
        {
            var identity = await _authService.GetByIdAsync(command.UserId);
            
            if (identity.IsVerified)
            {
                throw new AppException(HttpStatusCode.BadRequest, "User already verified.");   
            }

            var user = await _usersService.GetAsync(command.UserId);
            
            if (!string.IsNullOrEmpty(command.NewPhoneNumber) && user.PhoneNumber != command.NewPhoneNumber)
            {
                // Change user phone number if it was changed
                user.PhoneNumber = command.NewPhoneNumber;
                
                await _usersService.UpdateAsync(user);
            }
            
            var confirmCode = await _confirmCodeService.GenerateAsync(
                user: user, 
                type: ConfirmCodeType.ActivateIdentity, 
                option: ConfirmCodeOption.Numeric,
                length: 6);

            await _communicationService.SendVerificationSmsAsync(user, confirmCode.Code);
            
            return true;
        }
    }
}
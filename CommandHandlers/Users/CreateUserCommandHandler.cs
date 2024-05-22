using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Utils.PasswordUtil;
using WildHealth.Domain.Entities.Users;
using MediatR;
using WildHealth.Application.CommandHandlers.Auth.Flows;
using WildHealth.Application.CommandHandlers.Users.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;

namespace WildHealth.Application.CommandHandlers.Users
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, User>
    {
        private readonly IAuthService _authService;
        private readonly IPasswordUtil _passwordUtil;
        private readonly MaterializeFlow _materialization;

        public CreateUserCommandHandler(
            IAuthService authService, 
            IPasswordUtil passwordUtil,
            MaterializeFlow materialization)
        {
            _authService = authService;
            _passwordUtil = passwordUtil;
            _materialization = materialization;
        }

        public async Task<User> Handle(CreateUserCommand command, CancellationToken cancellationToken)
        {
            var identity = await _authService.GetByEmailOrNullAsync(command.Email);
            var emailIsInUse = new CheckIfEmailInUseCommandFlow(identity).Execute().InUse;
            var (passwordHash, passwordSalt) = _passwordUtil.CreatePasswordHash(command.Password);
            
            var result = await new CreateOrUpdateUserIdentityFlow(
                passwordHash: passwordHash, 
                passwordSalt: passwordSalt, 
                email: command.Email, 
                userType: command.UserType,
                isVerified: command.IsVerified, 
                firstName: command.FirstName, 
                lastName: command.LastName,
                practiceId: command.PracticeId,
                phoneNumber: command.PhoneNumber, 
                birthDate: command.BirthDate, 
                gender: command.Gender,
                billingAddress: command.BillingAddress,
                shippingAddress: command.ShippingAddress, 
                isRegistrationCompleted: command.IsRegistrationCompleted,
                note: command.Note, 
                marketingSms: command.MarketingSMS,
                meetingRecordingConsent: command.MeetingRecordingConsent,
                userIdentity: identity,
                emailIsInUse).Materialize(_materialization);

            return result.Select<UserIdentity>().User;
        }
    }
}
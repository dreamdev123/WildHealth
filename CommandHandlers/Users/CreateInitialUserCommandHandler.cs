using MediatR;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Extensions;
using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Users
{
    public class CreateInitialUserCommandHandler : IRequestHandler<CreateInitialUserCommand, User>
    {
        private readonly IPracticeService _practicesService;
        private readonly IAuthService _authService;

        public CreateInitialUserCommandHandler(
            IPracticeService practicesService,
            IAuthService authService)
        {
            _practicesService = practicesService;
            _authService = authService;
        }
        public async Task<User> Handle(CreateInitialUserCommand command, CancellationToken cancellationToken)
        {
            var practice = await _practicesService.GetAsync(command.PracticeId);

            await AssertEmailExistsAsync(command.Email);

            var identity = new UserIdentity(practice)
            {
                Email = command.Email,
                Type = command.UserType,
                User = new User(practice)
                {
                    Email = command.Email,
                    FirstName = command.FirstName.FirstCharToUpper(),
                    LastName = command.LastName.FirstCharToUpper(),
                    PhoneNumber = command.PhoneNumber,
                    Birthday = command.Birthday,
                    Gender = command.Gender,
                    IsRegistrationCompleted = false
                }
            };

            await _authService.CreateAsync(identity);

            return identity.User;
        }

        private async Task AssertEmailExistsAsync(string email)
        {
            var isEmailExists = await _authService.CheckIfEmailExistsAsync(email);

            if (isEmailExists)
            {
                throw new AppException(HttpStatusCode.BadRequest, $"Email address {email} is already registered.");
            }
        }
    }
}

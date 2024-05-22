using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Utils.PasswordGenerator;
using WildHealth.Common.Models.Auth;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Enums;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Insurances
{
    public class StartInsuranceRegistrationCommandHandler : IRequestHandler<StartInsuranceRegistrationCommand, AuthenticationResultModel>
    {
        private const string DefaultName = "UNKNOWN";

        private static readonly int[] DefaultLocations = 
        {
            0
        };
        
        private readonly IPasswordGenerator _passwordGenerator;
        private readonly IPracticeService _practiceService;
        private readonly IAuthService _authService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public StartInsuranceRegistrationCommandHandler(
            IPasswordGenerator passwordGenerator, 
            IPracticeService practiceService,
            IAuthService authService, 
            IMediator mediator, 
            ILogger<StartInsuranceRegistrationCommandHandler> logger)
        {
            _passwordGenerator = passwordGenerator;
            _practiceService = practiceService;
            _authService = authService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<AuthenticationResultModel> Handle(StartInsuranceRegistrationCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Insurance user registration with [Email] = {command.Email} started.");
            
            var practice = await _practiceService.GetAsync(command.PracticeId);

            var user = await CreateUserAsync(command.Email, command.PracticeId);

            var authResult = await AuthenticateUser(user, practice);
            
            _logger.LogInformation($"Insurance user registration with [Email] = {command.Email} finished.");
            
            return authResult;
        }
        
        #region private

        /// <summary>
        /// Creates and returns user
        /// </summary>
        /// <param name="email"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        private async Task<User> CreateUserAsync(string email, int practiceId)
        {
            var password = _passwordGenerator.Generate();
            
            var createUserCommand = new CreateUserCommand(
                firstName: DefaultName,
                lastName: DefaultName,
                email: email,
                phoneNumber: string.Empty,
                password: password,
                birthDate: null,
                gender: Gender.None,
                userType: UserType.Unspecified,
                practiceId: practiceId,
                billingAddress: new AddressModel(),
                shippingAddress: new AddressModel(),
                isVerified: true,
                isRegistrationCompleted: false
            );

            return await _mediator.Send(createUserCommand);
        }

        /// <summary>
        /// Authenticates user and returns result
        /// </summary>
        /// <param name="user"></param>
        /// <param name="practice"></param>
        /// <returns></returns>
        private async Task<AuthenticationResultModel> AuthenticateUser(User user, Practice practice)
        {
            return await _authService.Authenticate(
                identity: user.Identity,
                originPractice: practice,
                onBehalfPractice: null,
                permissions: Array.Empty<PermissionType>(),
                availableLocationsIds: DefaultLocations,
                defaultLocationId: null,
                external: null
            );
        }
        
        #endregion
    }
}
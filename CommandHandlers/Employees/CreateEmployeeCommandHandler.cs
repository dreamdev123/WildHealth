using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Events.Employees;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Permissions;
using WildHealth.Application.Services.Roles;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Application.CommandHandlers.Auth.Flows;
using WildHealth.Application.CommandHandlers.Employees.Flows;
using WildHealth.Application.CommandHandlers.Users.Flows;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Application.Extensions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Utils.PasswordUtil;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Options;
using WildHealth.Common.Options;

namespace WildHealth.Application.CommandHandlers.Employees
{
    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Employee>
    {
        private readonly IPermissionsService _permissionsService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly ILocationsService _locationsService;
        private readonly IDurableMediator _durableMediator;
        private readonly IRolesService _rolesService;
        private readonly IUsersService _usersService;
        private readonly MaterializeFlow _materialization;
        private readonly IAuthService _authService;
        private readonly IPasswordUtil _passwordUtil;
        private readonly RxNTOptions _rxntOptions;

        public CreateEmployeeCommandHandler(
            IPermissionsService permissionsService,
            IPermissionsGuard permissionsGuard,
            ILocationsService locationsService,
            IDurableMediator durableMediator,
            IRolesService rolesService,
            IUsersService usersService,
            MaterializeFlow materialization,
            IAuthService authService,
            IPasswordUtil passwordUtil,
            IOptions<RxNTOptions> rxntOptions)
        {
            _permissionsService = permissionsService;
            _locationsService = locationsService;
            _permissionsGuard = permissionsGuard;
            _durableMediator = durableMediator;
            _rolesService = rolesService;
            _usersService = usersService;
            _materialization = materialization;
            _authService = authService;
            _passwordUtil = passwordUtil;
            _rxntOptions = rxntOptions.Value;
        }

        public async Task<Employee> Handle(CreateEmployeeCommand command, CancellationToken cancellationToken)
        {
            var role = await _rolesService.GetByIdAsync(command.RoleId);
            var permissions = await _permissionsService.GetAsync(command.Permissions);
            var locations = await _locationsService.GetByIdsAsync(command.LocationIds, command.PracticeId);
            
            _permissionsGuard.AssertPermissions(role);
            _permissionsGuard.AssertPermissions(permissions);
            locations.ForEach(_permissionsGuard.AssertPermissions);
            
            var createOrUpdateUserFlow = await CreateOrUpdateUserAsync(command);

            var newEmployee = await createOrUpdateUserFlow.PipeTo(prevResult =>
                new CreateEmployeeFlow(locations, 
                    permissions, 
                    role, 
                    prevResult.Select<User>() ?? prevResult.Select<UserIdentity>().User, 
                    command.Credentials,
                    command.Npi,
                    _rxntOptions.EncryptionKey,
                    command.RxntUserName,
                    command.RxntPassword))
                .Materialize(_materialization)
                .Select<Employee>();

            await _durableMediator.Publish(new EmployeeCreatedEvent(
                EmployeeId: newEmployee.GetId(), 
                RegisterInSchedulerSystem: command.RegisterInSchedulerSystem
            ));
            
            return newEmployee;
        }

        private async Task<IMaterialisableFlow> CreateOrUpdateUserAsync(CreateEmployeeCommand command)
        {
            var user = await _usersService.GetByEmailAsync(command.Email);
            var identity = await _authService.GetByEmailOrNullAsync(command.Email);
            var emailIsInUse = new CheckIfEmailInUseCommandFlow(identity).Execute().InUse;

            return user is null ? CreateUser(command, identity, emailIsInUse) : UpdateUser(user, command, user.Identity, emailIsInUse);
        }

        private IMaterialisableFlow UpdateUser(User user, CreateEmployeeCommand command, UserIdentity identity, bool emailIsInUse)
        {
            return new UpdateUserFlow(
                user: user,
                firstName: command.FirstName,
                lastName: command.LastName,
                phoneNumber: user.PhoneNumber,
                birthday: user.Birthday,
                gender: command.Gender,
                billingAddress: user.BillingAddress,
                shippingAddress: user.ShippingAddress,
                userIdentity: identity,
                userType: UserType.Employee,
                emailIsInUse,
                command.Email);
        }

        private IMaterialisableFlow CreateUser(CreateEmployeeCommand command, UserIdentity identity, bool emailIsInUse)
        {
            var (passwordHash, passwordSalt) = _passwordUtil.CreatePasswordHash(Guid.NewGuid().ToString());
            
            return new CreateOrUpdateUserIdentityFlow(
                passwordHash: passwordHash, 
                passwordSalt: passwordSalt, 
                email: command.Email, 
                userType: UserType.Employee,
                isVerified: true,
                firstName: command.FirstName, 
                lastName: command.LastName,
                practiceId: command.PracticeId,
                phoneNumber: string.Empty,
                birthDate: null,
                gender: command.Gender,
                billingAddress: new AddressModel(),
                shippingAddress: new AddressModel(),
                isRegistrationCompleted: true,
                note: null,
                marketingSms: false,
                meetingRecordingConsent: false,
                userIdentity: identity,
                emailIsInUse: emailIsInUse);
        }
    }
}
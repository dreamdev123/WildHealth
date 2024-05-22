using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.CommandHandlers.Users.Flows;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Application.Events.Employees;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Permissions;
using WildHealth.Application.Services.Roles;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Address;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.CommandHandlers.Employees
{
    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Employee>
    {
        private readonly IEmployeeService _employeeService;
        private readonly IRolesService _rolesService;
        private readonly ILocationsService _locationsService;
        private readonly IPermissionsService _permissionsService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IDurableMediator _durableMediator;
        private readonly MaterializeFlow _materialization;
        private readonly IAuthService _authService;
        private readonly RxNTOptions _rxntOptions;
        
        public UpdateEmployeeCommandHandler(
            IEmployeeService employeeService,
            IRolesService rolesService,
            ILocationsService locationsService,
            IPermissionsService permissionsService,
            IPermissionsGuard permissionsGuard,
            IDurableMediator durableMediator,
            MaterializeFlow materialization, 
            IAuthService authService, 
            IOptions<RxNTOptions> rxntOptions)
        {
            _employeeService = employeeService;
            _rolesService = rolesService;
            _locationsService = locationsService;
            _permissionsService = permissionsService;
            _permissionsGuard = permissionsGuard;
            _materialization = materialization;
            _authService = authService;
            _rxntOptions = rxntOptions.Value;
            _durableMediator = durableMediator;
        }
        
        public async Task<Employee> Handle(UpdateEmployeeCommand command, CancellationToken cancellationToken)
        {
            var employee = await _employeeService.GetByIdAsync(command.Id);

            _permissionsGuard.AssertPermissions(employee);

            var newLocations = await _locationsService.GetByIdsAsync(command.LocationIds, employee.User.PracticeId);
            var newPermissions = await _permissionsService.GetAsync(command.Permissions);
            var newRole = await _rolesService.GetByIdAsync(command.RoleId);
            var identity = await _authService.GetByEmailAsync(command.Email);
            _permissionsGuard.AssertPermissions(newPermissions);
            _permissionsGuard.AssertPermissions(newRole);
                
            var updateEmployeeFlow = new UpdateEmployeeFlow(
                employee,
                newLocations.ToArray(),
                newPermissions.ToArray(),
                newRole,
                command.SchedulerAccountId,
                command.Credentials,
                command.Npi,
                _rxntOptions.EncryptionKey,
                command.RxntUserName,
                command.RxntPassword);
            
            var updateUserFlow = new UpdateUserFlow(
                employee.User,
                command.FirstName,
                command.LastName,
                employee.User.PhoneNumber,
                employee.User.Birthday,
                command.Gender,
                billingAddress: new Address(),
                shippingAddress: new Address(),
                identity,
                UserType.Employee,
                false,
                command.Email);
            
            await updateEmployeeFlow.PipeTo(updateUserFlow).Materialize(_materialization);
            await _durableMediator.Publish(new EmployeeUpdatedEvent(employee.GetId()));
            
            return employee;
        }
    }
}
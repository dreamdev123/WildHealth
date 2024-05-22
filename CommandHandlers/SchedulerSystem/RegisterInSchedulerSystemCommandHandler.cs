using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.SchedulerSystem;
using WildHealth.Application.Events.Schedulers;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Schedulers.Accounts;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.Scheduler;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Models.Employees;
using WildHealth.Settings;
using WildHealth.TimeKit.Clients.Models.Resources;
using MediatR;

namespace WildHealth.Application.CommandHandlers.SchedulerSystem
{
    public class RegisterInSchedulerSystemCommandHandler : IRequestHandler<RegisterInSchedulerSystemCommand>
    {
        private readonly ISchedulerAccountService _schedulerAccountService;
        private readonly IEmployeeService _employeeService;
        private readonly ISettingsManager _settingsManager;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public RegisterInSchedulerSystemCommandHandler(
            ILogger<RegisterInSchedulerSystemCommandHandler> logger,
            ISchedulerAccountService schedulerAccountService,
            IEmployeeService employeeService,
            ISettingsManager settingsManager,
            IMediator mediator)
        {
            _schedulerAccountService = schedulerAccountService;
            _employeeService = employeeService;
            _settingsManager = settingsManager;
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task Handle(RegisterInSchedulerSystemCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Registering employee in scheduler system with [Id]: {request.EmployeeId} was started");
            
            var employee = await _employeeService.GetByIdAsync(request.EmployeeId);

            var practiceId = employee.User.PracticeId;
            
            var migrationEnabled = await _settingsManager.GetSetting<bool>(SettingsNames.TimeKit.MigrationEnabled, practiceId);
            if (!migrationEnabled)
            {
                _logger.LogInformation($"Registering employee in scheduler system with [Id]: {request.EmployeeId} was skipped. Migration is not enabled");
                return;
            }

            var existingResource = await GetExistingAccountAsync(employee);

            if (existingResource is null)
            {
                var newSchedulerAccount = await RegisterAccountAsync(employee);
                
                await UpdateEmployeeAsync(newSchedulerAccount.Id, employee);
                
                await PublishEventsAsync(employee, newSchedulerAccount, cancellationToken);
            }
            else
            {
                await UpdateEmployeeAsync(existingResource.Id, employee);
            }
            
            _logger.LogInformation($"Registering employee in scheduler system with [Id]: {request.EmployeeId} has been successfully completed");
        }

        #region private

        private async Task<ResourceModel?> GetExistingAccountAsync(Employee employee)
        {
            var resources = await _schedulerAccountService.GetResourcesByEmailAsync(employee);

            if (resources.Length > 1)
            {
                _logger.LogError($"Scheduler system has more than 1 existing accounts with [Email]: {employee.User.Email}");
            }

            return resources.FirstOrDefault();
        }
        
        private async Task<SchedulerRegistrationResultModel> RegisterAccountAsync(Employee employee)
        {
            var offset = employee.Locations.FirstOrDefault()?.Location.Offset;

            var registerModel = new RegisterSchedulerAccountModel
            {
                PracticeId = employee.User.PracticeId,
                Email = employee.User.Email,
                FirstName = employee.User.FirstName,
                LastName = employee.User.LastName,
                TimeOffset = offset ?? 0
            };

            return await _schedulerAccountService.RegisterAccountAsync(registerModel);
        }

        private async Task UpdateEmployeeAsync(string schedulerAccountId, Employee employee)
        {
            var employeeDomain = EmployeeDomain.Create(employee);
            employeeDomain.UpdateSchedulerAccountId(schedulerAccountId);

            await _employeeService.UpdateAsync(employee);
        }
        
        private async Task PublishEventsAsync(Employee employee, SchedulerRegistrationResultModel schedulerAccount, CancellationToken cancellationToken)
        {
            await _mediator.Publish(
                new SchedulerAccountCreatedEvent(
                    practiceId: employee.User.PracticeId,
                    firstName: employee.User.FirstName,
                    email: employee.User.Email,
                    schedulerPassword: schedulerAccount.Password), 
                cancellationToken);
        }
        
        #endregion
    }
}
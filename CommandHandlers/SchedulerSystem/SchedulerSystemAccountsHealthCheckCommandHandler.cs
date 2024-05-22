using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.SchedulerSystem;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Notifications;
using WildHealth.Application.Services.Schedulers.Accounts;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using Constants = WildHealth.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Options;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;

namespace WildHealth.Application.CommandHandlers.SchedulerSystem;

public class SchedulerSystemAccountsHealthCheckCommandHandler : IRequestHandler<SchedulerSystemAccountsHealthCheckCommand>
{
    private readonly ILogger<SchedulerSystemAccountsHealthCheckCommandHandler> _logger;
    private readonly ISchedulerAccountService _schedulerAccountService;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly INotificationService _notificationService;
    private readonly IEmployeeService _employeeService;
    private readonly IOptions<PracticeOptions> _options;

    public SchedulerSystemAccountsHealthCheckCommandHandler(
        ILogger<SchedulerSystemAccountsHealthCheckCommandHandler> logger,
        ISchedulerAccountService schedulerAccountService,
        INotificationService notificationService,
        IFeatureFlagsService featureFlagsService,
        IEmployeeService employeeService,
        IOptions<PracticeOptions> options)
    {
        _schedulerAccountService = schedulerAccountService;
        _featureFlagsService = featureFlagsService;
        _notificationService = notificationService;
        _employeeService = employeeService;
        _logger = logger;
        _options = options;
    }

    public async Task Handle(SchedulerSystemAccountsHealthCheckCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start scheduler system accounts health check");
        
        var resources = await _schedulerAccountService.GetResourcesAsync(_options.Value.WildHealth);

        var issuedAccounts = resources
            .Where(x => x.Accounts.Any() 
                        && x.Accounts.First().Exception != null 
                            && x.Accounts.First().Exception.ToLower().Contains("exception"))
            .ToArray();
        
        _logger.LogInformation($"Scheduler system health check found {issuedAccounts.Length} issued accounts");

        foreach (var resource in issuedAccounts)
        {
            var employee = await _employeeService.GetBySchedulerAccountIdAsync(resource.Id);

            if (employee is null)
            {
                _logger.LogWarning($"Employee was not found by scheduler system, with [id] : {resource.Id}");
                continue;
            }
            
            var account = resource.Accounts.First();
            
            var notification = new TimekitAccountIssueNotification(
                employee: employee,
                accountType: account.Provider
            );
            
            _logger.LogWarning($"Employee with [Id] = {employee.GetId()} has issues with scheduler system");
            
            await _notificationService.CreateNotificationAsync(notification);
        }
    }
}
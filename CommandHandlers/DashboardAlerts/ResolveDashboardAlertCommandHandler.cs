using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.DashboardAlerts.Flows;
using WildHealth.Application.Commands.DashboardAlerts;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.DashboardAlerts;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.DateTimes;

namespace WildHealth.Application.CommandHandlers.DashboardAlerts;

public class ResolveDashboardAlertCommandHandler : IRequestHandler<ResolveDashboardAlertCommand, Unit>
{
    private readonly IDashboardAlertService _dashboardAlertService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUsersService _usersService;
    private readonly MaterializeFlow _materializeFlow;

    public ResolveDashboardAlertCommandHandler(
        IDashboardAlertService dashboardAlertService, 
        IDateTimeProvider dateTimeProvider,
        IUsersService usersService, 
        MaterializeFlow materializeFlow)
    {
        _dashboardAlertService = dashboardAlertService;
        _dateTimeProvider = dateTimeProvider;
        _usersService = usersService;
        _materializeFlow = materializeFlow;
    }

    public async Task<Unit> Handle(ResolveDashboardAlertCommand command, CancellationToken cancellationToken)
    {
        var alertToResolve = await _dashboardAlertService.GetByIdAsync(command.DashboardAlertId);

        var isAssignedToEmployee = await _dashboardAlertService.IsAssignedToEmployeeAsync(alertToResolve, command.EmployeeId);

        var isAssignedToCareCoordinator = await _dashboardAlertService.IsAssignedToCareCoordinatorAsync(alertToResolve, command.LocationIds);

        var user = await _usersService.GetByIdAsync(command.UserId);
        
        await new ResolveDashboardAlertFlow(
            alertToResolve, 
            isAssignedToEmployee, 
            isAssignedToCareCoordinator,
            _dateTimeProvider.UtcNow(),
            user?.UniversalId.ToString()).Materialize(_materializeFlow);
        
        return Unit.Value;
    }
}



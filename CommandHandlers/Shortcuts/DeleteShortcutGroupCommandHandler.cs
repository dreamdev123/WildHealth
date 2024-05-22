using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Shortcuts.Flows;
using WildHealth.Application.Commands.Shortcuts;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.ShortcutGroups;
using WildHealth.Domain.Entities.Shortcuts;
using MediatR;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Shortcuts;

public class DeleteShortcutGroupCommandHandler : IRequestHandler<DeleteShortcutGroupCommand, ShortcutGroup>
{
    private readonly IShortcutGroupService _shortcutGroupsService;
    private readonly IPermissionsGuard _permissionsGuard;
    private readonly IEmployeeService _employeesService;
    private readonly MaterializeFlow _materialization;

    public DeleteShortcutGroupCommandHandler(
        IShortcutGroupService shortcutGroupsService,
        IPermissionsGuard permissionsGuard,
        IEmployeeService employeesService,
        MaterializeFlow materialization)
    {
        _shortcutGroupsService = shortcutGroupsService;
        _permissionsGuard = permissionsGuard;
        _employeesService = employeesService;
        _materialization = materialization;
    }
    
    public async Task<ShortcutGroup> Handle(DeleteShortcutGroupCommand command, CancellationToken cancellationToken)
    {
        var employee = await _employeesService.GetByIdAsync(command.EmployeeId, EmployeeSpecifications.WithPermissions);
        
        var shortcutGroup = await _shortcutGroupsService.GetByIdAsync(
            id: command.Id,
            employeeId: command.EmployeeId, 
            practiceId: command.PracticeId
        );

        var flow = new DeleteShortcutGroupFlow(
            ShortcutGroup: shortcutGroup,
            Employee: employee,
            CheckPermission: _permissionsGuard.HasPermission
        );
        
        return await flow.Materialize(_materialization).Select<ShortcutGroup>();
    }
}
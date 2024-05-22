using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Shortcuts;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Application.CommandHandlers.Shortcuts.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.ShortcutGroups;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Shortcuts;

public class UpdateShortcutGroupCommandHandler : IRequestHandler<UpdateShortcutGroupCommand, ShortcutGroup>
{
    private readonly IShortcutGroupService _shortcutGroupsService;
    private readonly IPermissionsGuard _permissionsGuard;
    private readonly IEmployeeService _employeesService;
    private readonly MaterializeFlow _materialization;

    public UpdateShortcutGroupCommandHandler(
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
    
    public async Task<ShortcutGroup> Handle(UpdateShortcutGroupCommand command, CancellationToken cancellationToken)
    {
        var employee = await _employeesService.GetByIdAsync(command.EmployeeId, EmployeeSpecifications.WithPermissions);
        
        var shortcutGroup = await _shortcutGroupsService.GetByIdAsync(
            id: command.Id,
            employeeId: command.EmployeeId, 
            practiceId: command.PracticeId
        );

        if (shortcutGroup.Name != command.Name)
        {
            await _shortcutGroupsService.AssertNameIsUnique(
                name: command.Name,
                employeeId: command.EmployeeId,
                practiceId: command.PracticeId
            );
        }

        var flow = new UpdateShortcutGroupFlow(
            ShortcutGroup: shortcutGroup,
            Employee: employee,
            Name: command.Name,
            DisplayName: command.DisplayName,
            CheckPermission: _permissionsGuard.HasPermission
        );
        
        shortcutGroup = await flow.Materialize(_materialization).Select<ShortcutGroup>();
        
        return shortcutGroup;
    }
}
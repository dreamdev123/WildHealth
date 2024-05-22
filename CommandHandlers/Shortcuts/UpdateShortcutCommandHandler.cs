using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Shortcuts.Flows;
using WildHealth.Application.Commands.Shortcuts;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Shortcuts;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;
using WildHealth.Application.Services.ShortcutGroups;

namespace WildHealth.Application.CommandHandlers.Shortcuts;

public class UpdateShortcutCommandHandler : IRequestHandler<UpdateShortcutCommand, Shortcut>
{
    private readonly IShortcutGroupService _shortcutGroupService;
    private readonly IShortcutsService _shortcutsService;
    private readonly IPermissionsGuard _permissionsGuard;
    private readonly IEmployeeService _employeeService;
    private readonly MaterializeFlow _materialization;

    public UpdateShortcutCommandHandler(
        IShortcutGroupService shortcutGroupService,
        IShortcutsService shortcutsService, 
        IPermissionsGuard permissionsGuard, 
        IEmployeeService employeeService, 
        MaterializeFlow materialization)
    {
        _shortcutGroupService = shortcutGroupService;
        _shortcutsService = shortcutsService;
        _permissionsGuard = permissionsGuard;
        _employeeService = employeeService;
        _materialization = materialization;
    }
    
    public async Task<Shortcut> Handle(UpdateShortcutCommand command, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(command.EmployeeId, EmployeeSpecifications.WithPermissions);
             
        var shortcut = await _shortcutsService.GetAsync(
            id: command.Id,
            employeeId: command.EmployeeId,
            practiceId: command.PracticeId
        );

        if (shortcut.Name != command.Name)
        {
            await _shortcutsService.AssertNameIsUniqueAsync(
                name: command.Name,
                employeeId: command.EmployeeId,
                practiceId: command.PracticeId
            );
        }
        
        var shortcutGroup = shortcut.GroupId == command.GroupId
            ? shortcut.Group
            : await _shortcutGroupService.GetByIdAsync(
                id: command.GroupId,
                employeeId: command.EmployeeId,
                practiceId: command.PracticeId
            );

        var flow = new UpdateShortcutFlow(
            Name: command.Name,
            DisplayName: command.DisplayName,
            Content: command.Content,
            Shortcut: shortcut,
            Employee: employee,
            ShortcutGroup: shortcutGroup,
            CheckPermission: _permissionsGuard.HasPermission
        );
        
        return await flow.Materialize(_materialization).Select<Shortcut>();
    }
}
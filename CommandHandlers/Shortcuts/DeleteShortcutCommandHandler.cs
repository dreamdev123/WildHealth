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

namespace WildHealth.Application.CommandHandlers.Shortcuts;

public class DeleteShortcutCommandHandler : IRequestHandler<DeleteShortcutCommand, Shortcut>
{
    private readonly IShortcutsService _shortcutsService;
    private readonly IPermissionsGuard _permissionsGuard;
    private readonly IEmployeeService _employeeService;
    private readonly MaterializeFlow _materialization;

    public DeleteShortcutCommandHandler(
        IShortcutsService shortcutsService, 
        IPermissionsGuard permissionsGuard, 
        IEmployeeService employeeService, 
        MaterializeFlow materialization)
    {
        _shortcutsService = shortcutsService;
        _permissionsGuard = permissionsGuard;
        _employeeService = employeeService;
        _materialization = materialization;
    }
    
    public async Task<Shortcut> Handle(DeleteShortcutCommand command, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(command.EmployeeId, EmployeeSpecifications.WithPermissions);
             
        var shortcut = await _shortcutsService.GetAsync(
            id: command.Id,
            employeeId: command.EmployeeId,
            practiceId: command.PracticeId
        );

        var flow = new DeleteShortcutFlow(
            Shortcut: shortcut,
            Employee: employee,
            CheckPermission: _permissionsGuard.HasPermission
        );

        return await flow.Materialize(_materialization).Select<Shortcut>();
    }
}
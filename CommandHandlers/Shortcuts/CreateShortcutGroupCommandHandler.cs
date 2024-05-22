using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Shortcuts;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.ShortcutGroups;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Application.CommandHandlers.Shortcuts.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Utils.PermissionsGuard;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Shortcuts;

public class CreateShortcutGroupCommandHandler : IRequestHandler<CreateShortcutGroupCommand, ShortcutGroup>
{
    private readonly IShortcutGroupService _shortcutGroupsService;
    private readonly IPermissionsGuard _permissionsGuard;
    private readonly IEmployeeService _employeesService;
    private readonly IPracticeService _practiceService;
    private readonly MaterializeFlow _materialization;

    public CreateShortcutGroupCommandHandler(
        IShortcutGroupService shortcutGroupsService, 
        IPermissionsGuard permissionsGuard,
        IEmployeeService employeesService,
        IPracticeService practiceService,  
        MaterializeFlow materialization)
    {
        _shortcutGroupsService = shortcutGroupsService;
        _permissionsGuard = permissionsGuard;
        _employeesService = employeesService;
        _practiceService = practiceService;
        _materialization = materialization;
    }

    public async Task<ShortcutGroup> Handle(CreateShortcutGroupCommand command, CancellationToken cancellationToken)
    {
        var practice = await _practiceService.GetAsync(command.PracticeId);
        
        var employee = await _employeesService.GetByIdAsync(command.EmployeeId, EmployeeSpecifications.WithPermissions);

        await _shortcutGroupsService.AssertNameIsUnique(
            name: command.Name,
            employeeId: command.EmployeeId,
            practiceId: command.PracticeId
        );

        var flow = new CreateShortcutGroupFlow(
            Name: command.Name,
            DisplayName: command.DisplayName,
            IsPersonal: command.IsPersonal,
            Employee: employee,
            Practice: practice,
            CheckPermission: _permissionsGuard.HasPermission
        );
        
        return await flow.Materialize(_materialization).Select<ShortcutGroup>();
    }
}
using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Domain.Exceptions;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.CommandHandlers.Shortcuts.Flows;

public record DeleteShortcutGroupFlow(
    ShortcutGroup ShortcutGroup, 
    Employee Employee,
    Func<Employee, PermissionType, bool> CheckPermission): IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (ShortcutGroup.EmployeeId is null && !CheckPermission(Employee, PermissionType.Shortcuts))
        {
            throw new DomainException("You have no permissions");
        }
        
        if (ShortcutGroup.EmployeeId is not null && ShortcutGroup.EmployeeId != Employee.GetId())
        {
            throw new DomainException("You have no permissions");
        }
        
        return ShortcutGroup.Deleted();
    }
}
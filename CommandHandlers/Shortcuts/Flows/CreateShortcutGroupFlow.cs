using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Domain.Exceptions;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.CommandHandlers.Shortcuts.Flows;

public record CreateShortcutGroupFlow(
    string Name, 
    string DisplayName, 
    bool IsPersonal,
    Employee Employee, 
    Practice Practice, 
    Func<Employee, PermissionType, bool> CheckPermission): IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (IsPersonal && !CheckPermission(Employee, PermissionType.Shortcuts))
        {
            throw new DomainException("You have no permissions");
        }

        var shortcutGroup = new ShortcutGroup
        {
            Name = Name,
            DisplayName = DisplayName,
            EmployeeId = IsPersonal ? Employee.GetId() : null,
            PracticeId = Practice.GetId()
        };
        
        return shortcutGroup.Added();
    }
}
using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Domain.Exceptions;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.CommandHandlers.Shortcuts.Flows;

public record CreateShortcutFlow(
    string DisplayName,
    string Name,
    string Content,
    ShortcutGroup ShortcutGroup,
    Employee Employee,
    Func<Employee, PermissionType, bool> CheckPermission) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (ShortcutGroup.EmployeeId.HasValue && ShortcutGroup.EmployeeId != Employee.GetId())
        {
            throw new DomainException("You have no permissions");
        }
        
        if (!ShortcutGroup.EmployeeId.HasValue && !CheckPermission(Employee, PermissionType.Shortcuts))
        {
            throw new DomainException("You have no permissions");
        }
        
        var shortcut = new Shortcut
        {
            Name = Name,
            DisplayName = DisplayName,
            Content = Content,
            GroupId = ShortcutGroup.GetId()
        };

        return shortcut.Added();
    }
}
using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Domain.Exceptions;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.CommandHandlers.Shortcuts.Flows;

public record UpdateShortcutFlow(
    string DisplayName,
    string Name,
    string Content,
    Shortcut Shortcut,
    Employee Employee,
    ShortcutGroup ShortcutGroup,
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

        Shortcut.Name = Name;
        Shortcut.DisplayName = DisplayName;
        Shortcut.Content = Content;
        Shortcut.GroupId = ShortcutGroup.GetId();

        return Shortcut.Updated();
    }
}
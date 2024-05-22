using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.DashboardAlerts;

public class ResolveDashboardAlertCommand  : IRequest<Unit>, IValidatabe
{
    public ResolveDashboardAlertCommand(int dashboardAlertId, int employeeId, int? roleId, int[] locationIds, int userId)
    {
        DashboardAlertId = dashboardAlertId;
        EmployeeId = employeeId;
        RoleId = roleId;
        LocationIds = locationIds;
        UserId = userId;
    }

    public int UserId { get; set; }
    public int[] LocationIds { get; }
    public int? RoleId { get; }
    public int DashboardAlertId { get; }
    public int EmployeeId { get; }
    
    public bool IsValid() => new Validator().Validate(this).IsValid;
    public void Validate() => new Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<ResolveDashboardAlertCommand>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardAlertId).GreaterThan(0);
            RuleFor(x => x.EmployeeId).GreaterThan(0);
        }
    }
}
using MediatR;

namespace WildHealth.Application.Events.Employees
{
    public record EmployeeCreatedEvent(int EmployeeId, bool RegisterInSchedulerSystem) : INotification;
}
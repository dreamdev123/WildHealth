using MediatR;

namespace WildHealth.Application.Events.Employees;

public record EmployeeUpdatedEvent(int EmployeeId) : INotification;
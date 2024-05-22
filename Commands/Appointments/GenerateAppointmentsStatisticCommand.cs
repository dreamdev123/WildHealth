using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Commands.Appointments;

public class GenerateAppointmentsStatisticCommand : IRequest<AppointmentsStatisticModel>, IValidatabe
{
    public int EmployeeId { get; }
    public DateTime? AsOfDateTime { get; }
    public Employee? Employee { get; }
    
    public GenerateAppointmentsStatisticCommand(int employeeId, DateTime? asOfDateTime)
    {
        EmployeeId = employeeId;
        AsOfDateTime = asOfDateTime;
    }

    public GenerateAppointmentsStatisticCommand(Employee employee)
    {
        Employee = employee;
        EmployeeId = employee.GetId();
    }
    
    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GenerateAppointmentsStatisticCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).GreaterThan(0);
        }
    }

    #endregion
}
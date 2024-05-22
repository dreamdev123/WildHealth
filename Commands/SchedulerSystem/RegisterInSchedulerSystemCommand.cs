using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.SchedulerSystem
{
    /// <summary>
    /// Register employee in scheduler system and set account id into employee entity
    /// </summary>
    public class RegisterInSchedulerSystemCommand : IRequest, IValidatabe
    {
        public int EmployeeId { get; }

        public RegisterInSchedulerSystemCommand(int employeeId)
        {
            EmployeeId = employeeId;
        }

        #region validation
        
        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<RegisterInSchedulerSystemCommand>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeId).GreaterThan(0);
            }
        }
        
        #endregion
    }
}
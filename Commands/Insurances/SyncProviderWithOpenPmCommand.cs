using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Insurances;

public class SyncProviderWithOpenPmCommand : IRequest, IValidatabe
{
    public int EmployeeId { get; }
    
    public SyncProviderWithOpenPmCommand(int employeeId)
    {
        EmployeeId = employeeId;
    }
    
    #region validation

    private class Validator : AbstractValidator<SyncProviderWithOpenPmCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).GreaterThan(0);
        }
    }
    
    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}
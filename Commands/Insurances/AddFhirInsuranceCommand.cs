using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Address;
using WildHealth.Common.Models.Insurance;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Commands.Insurances;

public class AddFhirInsuranceCommand : IRequest<Insurance>, IValidatabe
{
    public InsuranceModel Insurance { get; }
    
    public StateModel[] States { get; }

    public AddFhirInsuranceCommand(InsuranceModel insurance, StateModel[] states)
    {
        Insurance = insurance;
        States = states;
    }

    #region validation
    private class Validator : AbstractValidator<AddFhirInsuranceCommand>
    {
        public Validator()
        {
            RuleFor(x => x.States).NotNull().NotEmpty();
            RuleFor(x => x.Insurance).NotNull();
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
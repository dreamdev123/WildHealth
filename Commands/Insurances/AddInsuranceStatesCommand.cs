using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Address;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Commands.Insurances;

public class AddInsuranceStatesCommand : IRequest<Insurance>, IValidatabe
{
    public int InsuranceId { get; }
    
    public StateModel[] States { get; }

    public AddInsuranceStatesCommand(int insuranceId, StateModel[] states)
    {
        InsuranceId = insuranceId;
        States = states;
    }

    #region validation

    private class Validator : AbstractValidator<AddInsuranceStatesCommand>
    {
        public Validator()
        {
            RuleFor(x => x.InsuranceId).GreaterThan(0);
            RuleFor(x => x.States).NotEmpty();
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
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Commands.Insurances;

public class VerifyCoverageCommand : IRequest<InsuranceVerification>, IValidatabe
{
    public int CoverageId { get; }

    public VerifyCoverageCommand(int coverageId)
    {
        CoverageId = coverageId;
    }

    #region validation

    private class Validator : AbstractValidator<VerifyCoverageCommand>
    {
        public Validator()
        {
            RuleFor(x => x.CoverageId).GreaterThan(0);
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
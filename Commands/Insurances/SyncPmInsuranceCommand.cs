using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Insurances;

public class SyncPmInsuranceCommand : IRequest, IValidatabe
{
    public string PmInsuranceName { get; set; }
    public int PracticeId { get; set; }

    public SyncPmInsuranceCommand(string pmInsuranceName, int practiceId)
    {
        PmInsuranceName = pmInsuranceName;
        PracticeId = practiceId;
    }
    
    #region validation

    private class Validator : AbstractValidator<SyncPmInsuranceCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PmInsuranceName).NotNull();
            RuleFor(x => x.PracticeId).GreaterThan(0);
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
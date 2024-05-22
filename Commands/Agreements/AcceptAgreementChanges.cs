using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Agreements;

public class AcceptAgreementChangesCommand : IRequest, IValidatabe
{
    public int PatientId { get; }
    
    public int AgreementId { get; }
    
    public AcceptAgreementChangesCommand(int patientId, int agreementId)
    {
        PatientId = patientId;
        AgreementId = agreementId;
    }

    #region Validation
    
    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    /// <returns></returns>
    public void Validate() => new Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<AcceptAgreementChangesCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.AgreementId).GreaterThan(0);
        }
    }
    
    #endregion
}
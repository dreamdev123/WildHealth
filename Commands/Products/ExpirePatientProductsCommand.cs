using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Products;

public class ExpirePatientProductsCommand : IRequest, IValidatabe
{
    public int PatientId { get; }

    public string Reason { get; }
    
    public ExpirePatientProductsCommand(int patientId, string reason)
    {
        PatientId = patientId;
        Reason = reason;
    }
    
    #region validation

    private class Validator : AbstractValidator<ExpirePatientProductsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.Reason).NotNull().NotEmpty();
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
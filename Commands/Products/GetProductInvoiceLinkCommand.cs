using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Products;

public class GetProductInvoiceLinkCommand : IRequest<string>, IValidatabe
{
    public int PatientId { get; }
    
    public int PatientProductId { get; }
    
    public string Purpose { get; }
    
    public GetProductInvoiceLinkCommand(
        int patientId,
        int patientProductId, 
        string purpose)
    {
        PatientId = patientId;
        PatientProductId = patientProductId;
        Purpose = purpose;
    }
    
    #region validation

    private class Validator : AbstractValidator<GetProductInvoiceLinkCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.PatientProductId).GreaterThan(0);
            RuleFor(x => x.Purpose).NotNull().NotEmpty();
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
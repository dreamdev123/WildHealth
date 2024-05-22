using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Insurances;

public class CreatePatientResponsibilityInvoiceCommand : IRequest
{
    public int NoteId { get; }
    
    public decimal CopayAmount { get; }
    
    public decimal CoinsuranceAmount { get; }
    
    public decimal DeductibleAmount { get; }

    public CreatePatientResponsibilityInvoiceCommand(
        int noteId,
        decimal copayAmount, 
        decimal coinsuranceAmount,
        decimal deductibleAmount)
    {
        NoteId = noteId;
        CopayAmount = copayAmount;
        CoinsuranceAmount = coinsuranceAmount;
        DeductibleAmount = deductibleAmount;
    }
    
    #region validation

    private class Validator : AbstractValidator<CreatePatientResponsibilityInvoiceCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NoteId).GreaterThan(0);
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
    /// <returns></returns>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}
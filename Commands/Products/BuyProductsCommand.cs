using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Products;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Products;

public class BuyProductsCommand : IRequest<PatientProduct[]>, IValidatabe
{
    public int PatientId { get; }
    
    public bool IsPaidByDefaultEmployer { get; }
    
    public (ProductType type, int quantity)[] Products { get; }
    
    public BuyProductsCommand(
        int patientId, 
        (ProductType type, int quantity)[] products,
        bool isPaidByDefaultEmployer)
    {
        PatientId = patientId;
        Products = products;
        IsPaidByDefaultEmployer = isPaidByDefaultEmployer;
    }

    #region private

    private class Validator : AbstractValidator<BuyProductsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.Products).NotNull().NotEmpty();
            RuleForEach(x => x.Products)
                .Must(x => x.quantity > 0);
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
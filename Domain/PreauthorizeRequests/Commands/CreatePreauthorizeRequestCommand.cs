using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Users;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Commands;

public class CreatePreauthorizeRequestCommand : IRequest<PreauthorizeRequest>, IValidatabe
{
    public string FirstName { get; }
    
    public string LastName { get; }
    
    public string Email { get; }
    
    public int PaymentPlanId { get; }
    
    public int PaymentPeriodId { get; }
    
    public int PaymentPriceId { get; }
    
    public int? EmployerProductId { get; }
    
    public int PracticeId { get; set; }
    
    public CreatePreauthorizeRequestCommand(
        string firstName, 
        string lastName, 
        string email, 
        int paymentPlanId, 
        int paymentPeriodId, 
        int paymentPriceId, 
        int? employerProductId,
        int practiceId)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PaymentPlanId = paymentPlanId;
        PaymentPeriodId = paymentPeriodId;
        PaymentPriceId = paymentPriceId;
        EmployerProductId = employerProductId;
        PracticeId = practiceId;
    }

    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    public class Validator : AbstractValidator<CreatePreauthorizeRequestCommand>
    {
        public Validator()
        {
            RuleFor(x => x.FirstName).NotNull().NotEmpty();
            RuleFor(x => x.LastName).NotNull().NotEmpty();
            RuleFor(x => x.Email).NotNull().NotEmpty();
            RuleFor(x => x.PaymentPlanId).GreaterThan(0);
            RuleFor(x => x.PaymentPeriodId).GreaterThan(0);
            RuleFor(x => x.PaymentPriceId).GreaterThan(0);
            RuleFor(x => x.PracticeId).GreaterThan(0);
            RuleFor(x => x.EmployerProductId)
                .GreaterThan(0)
                .When(x => x.EmployerProductId.HasValue);
        }
    }

    #endregion
}
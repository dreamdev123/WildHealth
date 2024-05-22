using System.Linq;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Payments;
using WildHealth.Domain.Entities.Payments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Payments;

public class CreateDefaultPaymentPlanCommand : IRequest<PaymentPlan>, IValidatabe
{
    public string Name { get; }
    public string DisplayName { get; }
    public string Title { get; }
    public bool IsActive { get; } = false;
    public int DesiredId { get; }
    public int PracticeId { get; }
    public int PaymentPlanTemplateId { get; }
    public int PeriodInMonths { get; } = 12;
    public CreatePaymentPriceModel[] Prices { get; }
    public CreatePaymentPlanInclusion[] Inclusions { get; }
    public bool IncludeDefaultAddOns { get; } = true;
    public string StripeProductId { get; }
    public bool CanBeActivated { get; }
    public bool IsPrecisionCarePackageFlow { get; }

    public CreateDefaultPaymentPlanCommand(
        string name,
        string displayName,
        string title,
        bool isActive,
        int desiredId,
        int practiceId,
        int paymentPlanTemplateId,
        int periodInMonths,
        CreatePaymentPriceModel[] prices,
        CreatePaymentPlanInclusion[] inclusions,
        bool includeDefaultAddOns,
        string stripeProductId,
        bool canBeActivated,
        bool isPrecisionCarePackageFlow
        )
    {
        Name = name;
        DisplayName = displayName;
        Title = title;
        IsActive = isActive;
        DesiredId = desiredId;
        PracticeId = practiceId;
        PaymentPlanTemplateId = paymentPlanTemplateId;
        PeriodInMonths = periodInMonths;
        Prices = prices;
        Inclusions = inclusions;
        IncludeDefaultAddOns = includeDefaultAddOns;
        StripeProductId = stripeProductId;
        CanBeActivated = canBeActivated;
        IsPrecisionCarePackageFlow = isPrecisionCarePackageFlow;
    }
    
    #region validation

    private class Validator : AbstractValidator<CreateDefaultPaymentPlanCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotNull();
            RuleFor(x => x.DisplayName).NotNull();
            RuleFor(x => x.Title).NotNull();
            RuleFor(x => x.PracticeId).GreaterThan(0);
            RuleFor(x => x.PeriodInMonths).GreaterThan(0);
            RuleFor(x => x.Prices.Count()).GreaterThan(0);
            RuleFor(x => x.Prices.All(o => o.OriginalPrice > 0));
            RuleFor(x => x.Inclusions.Count()).GreaterThan(0);
            RuleFor(x => x.Inclusions.All(o => o.Count > 0));
            RuleFor(x => x.StripeProductId).NotNull();
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
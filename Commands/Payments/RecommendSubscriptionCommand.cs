using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Payments;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.Commands.Payments;

public class RecommendSubscriptionCommand : IRequest<RecommendPaymentPriceModel>, IValidatabe
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string PlanName { get; set; }
    public string CouponCode { get; set; }
    public PaymentStrategy PaymentStrategy { get; set; }
    public bool IsInsurance { get; set; }

    public RecommendSubscriptionCommand(
        string planName,
        PaymentStrategy paymentStrategy,
        bool isInsurance,
        string couponCode,
        DateTime? startDate,
        DateTime? endDate)
    {
        PlanName = planName;
        PaymentStrategy = paymentStrategy;
        IsInsurance = isInsurance;
        CouponCode = couponCode;
        StartDate = startDate;
        EndDate = endDate;
    }
    
    
    #region validation

    private class Validator : AbstractValidator<RecommendSubscriptionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PlanName).NotNull();
            RuleFor(x => x.PaymentStrategy).NotNull();
            RuleFor(x => x.IsInsurance).NotNull();
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
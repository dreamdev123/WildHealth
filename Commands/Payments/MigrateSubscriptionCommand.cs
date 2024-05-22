using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.Commands.Payments;

public class MigrateSubscriptionCommand : IRequest<bool>, IValidatabe
{
    public int FromSubscriptionId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string PlanName { get; set; }
    public string CouponCode { get; set; }
    public int? EmployerProductId { get; set; }
    public PaymentStrategy PaymentStrategy { get; set; }
    public bool IsInsurance { get; set; }

    public MigrateSubscriptionCommand(
        int fromSubscriptionId,
        string planName,
        PaymentStrategy paymentStrategy,
        bool isInsurance,
        string couponCode,
        int? employerProductId,
        DateTime? startDate,
        DateTime? endDate)
    {
        FromSubscriptionId = fromSubscriptionId;
        PlanName = planName;
        PaymentStrategy = paymentStrategy;
        IsInsurance = isInsurance;
        CouponCode = couponCode;
        EmployerProductId = employerProductId;
        StartDate = startDate;
        EndDate = endDate;
    }
    
    
    #region validation

    private class Validator : AbstractValidator<MigrateSubscriptionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.FromSubscriptionId).NotNull();
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
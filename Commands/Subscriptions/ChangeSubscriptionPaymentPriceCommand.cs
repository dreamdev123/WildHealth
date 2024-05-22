using System;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Subscriptions;
using FluentValidation;
using WildHealth.Domain.Entities.Payments;
using MediatR;

public class ChangeSubscriptionPaymentPriceCommand : IRequest<Subscription>, IValidatabe
{
    public int CurrentSubscriptionId { get; set; }
    public int NewPaymentPriceId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? CouponCode { get; set; }
    public int? EmployerProductId { get; set; }

    public ChangeSubscriptionPaymentPriceCommand(
        DateTime? startDate,
        DateTime? endDate,
        int currentSubscriptionId,
        int newPaymentPriceId,
        string? couponCode,
        int? employerProductId)
    {
        StartDate = startDate;
        EndDate = endDate;
        CurrentSubscriptionId = currentSubscriptionId;
        NewPaymentPriceId = newPaymentPriceId;
        CouponCode = couponCode;
        EmployerProductId = employerProductId;
    }

    #region private 

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<ChangeSubscriptionPaymentPriceCommand>
    {
        public Validator()
        {
            RuleFor(x => x.CurrentSubscriptionId)
                .GreaterThan(0);

            RuleFor(x => x.NewPaymentPriceId)
                .GreaterThan(0);
        }
    }

    #endregion
}
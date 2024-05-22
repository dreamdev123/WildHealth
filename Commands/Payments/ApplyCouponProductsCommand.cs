using WildHealth.Domain.Entities.Patients;
using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Payments
{
    public class ApplyCouponProductsCommand : IRequest<bool>, IValidatabe
    {
        public Patient Patient { get; }

        public int? PaymentPriceId { get; }

        public ApplyCouponProductsCommand(
            Patient patient,
            int? paymentPriceId)
        {
            Patient = patient;
            PaymentPriceId = paymentPriceId;
        }

        #region validation

        private class Validator : AbstractValidator<ApplyCouponProductsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Patient).NotNull();
                RuleFor(x => x.PaymentPriceId).GreaterThan(0).When(x => x.PaymentPriceId.HasValue);
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
}

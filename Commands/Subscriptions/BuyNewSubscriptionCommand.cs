using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Agreements;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using FluentValidation;
using MediatR;
using WildHealth.Domain.Entities.EmployerProducts;

namespace WildHealth.Application.Commands.Subscriptions
{
    /// <summary>
    /// Represents buy new subscription command
    /// </summary>
    public class BuyNewSubscriptionCommand : IRequest<Subscription>, IValidatabe
    {
        public Patient Patient { get; }
        
        public int PaymentPriceId { get; }
        
        public int PaymentPeriodId { get; }
        
        public EmployerProduct EmployerProduct { get; }

        public ConfirmAgreementModel[]? Agreements { get; }

        public bool ConfirmAgreements { get; }

        public bool NoStartupFee { get; }

        public string? PromoCode { get; }

        public BuyNewSubscriptionCommand(
            Patient patient,
            int paymentPriceId,
            int paymentPeriodId,
            ConfirmAgreementModel[]? agreements, 
            EmployerProduct employerProduct, 
            bool confirmAgreements = true,
            bool noStartupFee = false,
            string? promoCode = null)
        {
            Patient = patient;
            PaymentPriceId = paymentPriceId;
            PaymentPeriodId = paymentPeriodId;
            Agreements = agreements;
            EmployerProduct = employerProduct;
            ConfirmAgreements = confirmAgreements;
            NoStartupFee = noStartupFee;
            PromoCode = promoCode;
        }

        #region validation

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);


        private class Validator : AbstractValidator<BuyNewSubscriptionCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Patient).NotNull();

                RuleFor(x => x.PaymentPriceId).GreaterThan(0);

                RuleFor(x => x.PaymentPeriodId).GreaterThan(0);

                RuleFor(x => x.Agreements)
                    .NotNull()
                    .When(x => x.ConfirmAgreements);
            }
        }

        #endregion
    }
}
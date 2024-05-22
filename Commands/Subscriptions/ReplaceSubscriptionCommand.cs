using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Agreements;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Commands.Subscriptions
{
    /// <summary>
    /// Represents command for replacing subscription by new
    /// </summary>
    public class ReplaceSubscriptionCommand : IRequest<Subscription>, IValidatabe
    {
        public int PatientId { get; }
        
        public int PaymentPriceId { get; }
        
        public int PaymentPeriodId { get; }
        
        public int[] AddOnIds { get; }
        
        public int? FounderId { get; }

        public ConfirmAgreementModel[] Agreements { get; }

        public string? PromoCode { get; }
        
        public ReplaceSubscriptionCommand(int patientId,
            int paymentPriceId,
            int paymentPeriodId,
            int[] addOnIds,
            int? founderId,
            ConfirmAgreementModel[] agreements, 
            string? promoCode = default)
        {
            PatientId = patientId;
            PaymentPriceId = paymentPriceId;
            PaymentPeriodId = paymentPeriodId;
            AddOnIds = addOnIds;
            FounderId = founderId;
            Agreements = agreements;
            PromoCode = promoCode;
        }

        #region validation
        
        private class Validator : AbstractValidator<ReplaceSubscriptionCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                
                RuleFor(x => x.PaymentPriceId).GreaterThan(0);
                
                RuleFor(x => x.PaymentPeriodId).GreaterThan(0);
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
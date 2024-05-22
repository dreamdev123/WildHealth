using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Payments
{
    public class OfferPaymentPlanCommand : IRequest, IValidatabe
    {
        public int PaymentPlanId { get; }
        
        public int PaymentPeriodId { get; }
        
        public int PracticeId { get; }
        
        public int PatientId { get; }
        
        public OfferPaymentPlanCommand(
            int paymentPlanId, 
            int paymentPeriodId,
            int practiceId,
            int patientId)
        {
            PaymentPlanId = paymentPlanId;
            PaymentPeriodId = paymentPeriodId;
            PracticeId = practiceId;
            PatientId = patientId;
        }

        #region validation

        private class Validator : AbstractValidator<OfferPaymentPlanCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PaymentPlanId).GreaterThan(0);
                RuleFor(x => x.PaymentPeriodId).GreaterThan(0);
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.PatientId).GreaterThan(0);
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
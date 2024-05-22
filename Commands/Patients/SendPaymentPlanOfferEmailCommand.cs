using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Commands.Patients
{
    public class SendPaymentPlanOfferEmailCommand : IRequest, IValidatabe
    {
        public Patient Patient { get; }
        
        public PaymentPlan PaymentPlan { get; }

        public PaymentPeriod PaymentPeriod { get; }
        
        public int PracticeId { get; }
        
        public SendPaymentPlanOfferEmailCommand(
            Patient patient, 
            PaymentPlan paymentPlan, 
            PaymentPeriod paymentPeriod, 
            int practiceId)
        {
            Patient = patient;
            PaymentPlan = paymentPlan;
            PaymentPeriod = paymentPeriod;
            PracticeId = practiceId;
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

        private class Validator : AbstractValidator<SendPaymentPlanOfferEmailCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.PaymentPlan).NotNull();
                RuleFor(x => x.Patient).NotNull();
                RuleFor(x => x.PaymentPeriod).NotNull();
            }
        }

        #endregion
    }
}
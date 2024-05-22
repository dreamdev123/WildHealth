using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Payments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Subscriptions
{
    public class ActivateSubscriptionCommand: IRequest<Subscription>, IValidatabe
    {
        public int PatientId { get; }
        
        public int PaymentPriceId { get; }
        
        public DateTime StartDate { get; }
        
        public ActivateSubscriptionCommand(
            int patientId, 
            int paymentPriceId, 
            DateTime startDate)
        {
            PatientId = patientId;
            PaymentPriceId = paymentPriceId;
            StartDate = startDate;
        }
        
        #region validation

        private class Validator : AbstractValidator<ActivateSubscriptionCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.PaymentPriceId).GreaterThan(0);
                RuleFor(x => x.StartDate).NotEmpty();
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
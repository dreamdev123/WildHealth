using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using Subscription = Stripe.Subscription;

namespace WildHealth.Application.Commands.Patients
{
    public class SendWelcomeEmailCommand : IRequest, IValidatabe
    {
        public int PatientId { get; }
        public int SubscriptionId { get; }
        public int[] SelectedAddOnIds { get; }
        
        public SendWelcomeEmailCommand(int patientId, int subscriptionId, int[] selectedAddOnIds)
        {
            PatientId = patientId;
            SubscriptionId = subscriptionId;
            SelectedAddOnIds = selectedAddOnIds;
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

        private class Validator : AbstractValidator<SendWelcomeEmailCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).NotEmpty();
                RuleFor(x => x.SubscriptionId).NotEmpty();
                RuleFor(x => x.SelectedAddOnIds).ForEach(x => x.GreaterThan(0));
            }
        }

        #endregion
    }
}
using System.Collections.Generic;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Subscriptions;

namespace WildHealth.Application.Commands.Subscriptions
{
    public class SendRenewalSubscriptionReportCommand : IRequest, IValidatabe
    {
        public IEnumerable<RenewSubscriptionReportModel> RenewedSubscription { get; }

        public int PracticeId { get; }

        public SendRenewalSubscriptionReportCommand(
            IEnumerable<RenewSubscriptionReportModel> renewedSubscription,
            int practiceId)
        {
            RenewedSubscription = renewedSubscription;
            PracticeId = practiceId;
        }
        
        #region validation
        
        private class Validator : AbstractValidator<SendRenewalSubscriptionReportCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.RenewedSubscription).NotNull();
                RuleFor(x => x.RenewedSubscription)
                    .ForEach(x => x.NotNull());

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
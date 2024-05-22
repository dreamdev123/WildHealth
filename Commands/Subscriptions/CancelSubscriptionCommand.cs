using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.Commands.Subscriptions
{
    public class CancelSubscriptionsCommand : IRequest<Subscription>, IValidatabe
    {
        public int Id { get; }

        public CancellationReasonType ReasonType { get; }
        
        public string Reason { get; }

        public DateTime? Date { get; }

        public CancelSubscriptionsCommand(
            int id,
            CancellationReasonType reasonType,
            string reason,
            DateTime? date)
        {
            Id = id;
            ReasonType = reasonType;
            Reason = reason;
            Date = date;
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

        private class Validator : AbstractValidator<CancelSubscriptionsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.ReasonType).IsInEnum();
                RuleFor(x => x.Reason).MaximumLength(100);
            }
        }

        #endregion
    }
}
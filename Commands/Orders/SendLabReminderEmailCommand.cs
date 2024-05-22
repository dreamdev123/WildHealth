using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Orders
{
    public class SendLabReminderEmailCommand : IRequest<Unit>, IValidatabe
    {
        public int Orderid { get; set; }

        public SendLabReminderEmailCommand(int orderId)
        {
            Orderid = orderId;
        }

        #region validation

        private class Validator : AbstractValidator<SendLabReminderEmailCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Orderid).GreaterThan(0);
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

using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class SendDnaOrderShippedEmailCommand : IRequest, IValidatabe
    {
        public DnaOrder Order { get; }
        
        public SendDnaOrderShippedEmailCommand(DnaOrder order)
        {
            Order = order;
        }

        #region validation

        private class Validator : AbstractValidator<SendDnaOrderShippedEmailCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Order).NotNull();
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
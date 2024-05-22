using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class OrderCreatedAtCheckoutCommand : IRequest<bool>, IValidatabe
    {
        public Order Order { get; }
        
        public OrderCreatedAtCheckoutCommand(Order order)
        {
            Order = order;
        }
        
        #region validation

        private class Validator : AbstractValidator<OrderCreatedAtCheckoutCommand>
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
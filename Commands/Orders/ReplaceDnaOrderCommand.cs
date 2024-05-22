using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class ReplaceDnaOrderCommand : IRequest<(DnaOrder replacedOrder, DnaOrder newOrder)>, IValidatabe
    {
        public int Id { get; }
        
        public string Reason { get; }
        
        public ReplaceDnaOrderCommand(int id, string reason)
        {
            Id = id;
            Reason = reason;
        }

        #region validation

        private class Validator : AbstractValidator<ReplaceDnaOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.Reason).NotNull().NotEmpty();
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
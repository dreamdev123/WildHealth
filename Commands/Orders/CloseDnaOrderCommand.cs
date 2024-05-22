using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class CloseDnaOrderCommand : IRequest<DnaOrder>, IValidatabe
    {
        public int Id { get; }
        
        public bool IsSucceeded { get; }
        
        public CloseDnaOrderCommand(int id, bool isSucceeded)
        {
            Id = id;
            IsSucceeded = isSucceeded;
        }
        
        #region validation

        private class Validator : AbstractValidator<CloseDnaOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
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
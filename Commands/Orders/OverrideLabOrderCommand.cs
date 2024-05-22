using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class OverrideLabOrderCommand : IRequest<LabOrder>, IValidatabe
    {
        public int Id { get; }
        public string OverrideReason { get; }
        public int OverrideById { get; }

        public OverrideLabOrderCommand(int id, string overrideReason, int overrideById)
        {
            Id = id;
            OverrideReason = overrideReason;
            OverrideById = overrideById;
        }
        
        #region validation

        private class Validator : AbstractValidator<OverrideLabOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.OverrideReason).NotNull().NotEmpty();
                RuleFor(x => x.OverrideById).GreaterThan(0);
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
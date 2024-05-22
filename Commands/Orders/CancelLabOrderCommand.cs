using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class CancelLabOrderCommand : IRequest<LabOrder>, IValidatabe
    {
        public int Id { get; }
        public string CancellationReason { get; }
        public int CancelledById { get; }

        public CancelLabOrderCommand(int id, string cancellationReason, int cancelledById)
        {
            Id = id;
            CancellationReason = cancellationReason;
            CancelledById = cancelledById;
        }
        
        #region validation

        private class Validator : AbstractValidator<CancelLabOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.CancellationReason).NotNull().NotEmpty();
                RuleFor(x => x.CancelledById).GreaterThan(0);
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
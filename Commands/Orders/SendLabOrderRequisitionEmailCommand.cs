using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class SendLabOrderRequisitionEmailCommand : IRequest, IValidatabe
    {
        public SendLabOrderRequisitionEmailCommand(LabOrder order)
        {
            Order = order;
        }

        public LabOrder Order { get; }
        
        #region validation

        private class Validator : AbstractValidator<SendLabOrderRequisitionEmailCommand>
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
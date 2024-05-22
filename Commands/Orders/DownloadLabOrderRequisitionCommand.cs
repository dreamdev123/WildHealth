using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class DownloadLabOrderRequisitionCommand : IRequest<(byte[], string)>, IValidatabe
    {
        public int OrderId { get; }
        
        public DownloadLabOrderRequisitionCommand(int orderId)
        {
            OrderId = orderId;
        }
        
        #region validation

        private class Validator : AbstractValidator<DownloadLabOrderRequisitionCommand>
        {
            public Validator()
            {
                RuleFor(x => x.OrderId).GreaterThan(0);
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
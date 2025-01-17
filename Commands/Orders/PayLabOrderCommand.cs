using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class PayLabOrderCommand : IRequest<LabOrder>, IValidatabe
    {
        public int Id { get; }

        public bool ShouldSendOrderInvoiceEmail { get; }
        
        public PayLabOrderCommand(int id, bool shouldSendOrderInvoiceEmail)
        {
            Id = id;
            ShouldSendOrderInvoiceEmail = shouldSendOrderInvoiceEmail;
        }
        
        #region validation

        private class Validator : AbstractValidator<PayLabOrderCommand>
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
using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class MarkLabOrderAsPaidCommand : IRequest<LabOrder>, IValidatabe
    {
        public LabOrder Order { get;  }
        
        public string PaymentId { get; }
        
        public DateTime PaymentDate { get; }
        
        public MarkLabOrderAsPaidCommand(
            LabOrder order, 
            string paymentId, 
            DateTime paymentDate)
        {
            Order = order;
            PaymentId = paymentId;
            PaymentDate = paymentDate;
        }

        #region validation

        private class Validator: AbstractValidator<MarkLabOrderAsPaidCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Order).NotNull();
                RuleFor(x => x.PaymentId).NotNull().NotEmpty();
                RuleFor(x => x.PaymentDate).NotEmpty();
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
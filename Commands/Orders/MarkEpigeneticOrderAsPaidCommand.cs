using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;

namespace WildHealth.Application.Commands.Orders
{
    public class MarkEpigeneticOrderAsPaidCommand : IRequest<EpigeneticOrder>, IValidatabe
    {
        public EpigeneticOrder Order { get;  }
        
        public string PaymentId { get; }
        
        public DateTime PaymentDate { get; }
        
        public MarkEpigeneticOrderAsPaidCommand(
            EpigeneticOrder order, 
            string paymentId, 
            DateTime paymentDate)
        {
            Order = order;
            PaymentId = paymentId;
            PaymentDate = paymentDate;
        }

        #region validation

        private class Validator: AbstractValidator<MarkEpigeneticOrderAsPaidCommand>
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
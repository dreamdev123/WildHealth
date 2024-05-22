using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class CreateLabOrderCommand : IRequest<LabOrder>, IValidatabe
    {
        public int PatientId { get; }
        
        public string OrderNumber { get; }
        
        public int[] AddOnIds { get; }
        public CreateLabOrderCommand(
            int patientId,
            string orderNumber,
            int[] addOnIds)
        {
            PatientId = patientId;
            AddOnIds = addOnIds;
            OrderNumber = orderNumber;
        }
        
        #region Validation
        
        private class Validator : AbstractValidator<CreateLabOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.AddOnIds).NotNull().NotEmpty();
                RuleForEach(x => x.AddOnIds).GreaterThan(0);
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
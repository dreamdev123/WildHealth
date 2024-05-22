using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Inputs
{
    public class FinalizeLabOrderCommand : IRequest, IValidatabe
    {
        public int PatientId { get; }
        public string OrderNumber { get; }
        
        public FinalizeLabOrderCommand(
            int patientId, 
            string orderNumber)
        {
            PatientId = patientId;
            OrderNumber = orderNumber;
        }

        #region validation

        private class Validator : AbstractValidator<FinalizeLabOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.OrderNumber).NotNull();
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
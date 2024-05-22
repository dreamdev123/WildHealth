using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class CreateDnaOrderCommand : IRequest<DnaOrder>, IValidatabe    
    {
        public int PatientId { get; }
        
        public int[] AddOnIds { get; }
        
        public bool ProcessPayment { get; }
        
        public bool IsManual { get; set; }
        
        public string? BarCode { get; set; }
        
        public string? OutboundShippingCode { get; set; }
        
        public string? ReturnShippingCode { get; set; }
        
        
        public CreateDnaOrderCommand(
            int patientId,
            int[] addOnIds, 
            bool isManual,
            string? barCode,
            string? outboundShippingCode,
            string? returnShippingCode,
            bool processPayment)
        {
            PatientId = patientId;
            AddOnIds = addOnIds;
            ProcessPayment = processPayment;
            IsManual = isManual;
            BarCode = barCode;
            OutboundShippingCode = outboundShippingCode;
            ReturnShippingCode = returnShippingCode;
        }
        
        #region Validation
        
        private class Validator : AbstractValidator<CreateDnaOrderCommand>
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
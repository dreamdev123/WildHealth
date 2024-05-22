using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class DropshippingDnaOrderCommand : IRequest<DnaOrder>, IValidatabe
    {
        public int Id { get; }
        
        public string Number { get; }
        
        public string Barcode { get; }
        
        public string PatientShippingNumber { get; }

        public string LaboratoryShippingNumber { get; }
        
        public DropshippingDnaOrderCommand(
          int id,
          string number,
          string barcode,
          string patientShippingNumber,
          string laboratoryShippingNumber)
        {
            Id = id;
            Number = number;
            Barcode = barcode;
            PatientShippingNumber = patientShippingNumber;
            LaboratoryShippingNumber = laboratoryShippingNumber;
        }

        #region validation

        private class Validator : AbstractValidator<DropshippingDnaOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.Number).NotNull().NotEmpty();
                RuleFor(x => x.Barcode).NotNull().NotEmpty();
                RuleFor(x => x.PatientShippingNumber).NotNull().NotEmpty();
                RuleFor(x => x.LaboratoryShippingNumber).NotNull().NotEmpty();
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
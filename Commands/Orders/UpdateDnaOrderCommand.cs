using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class UpdateDnaOrderCommand : IRequest<DnaOrder>, IValidatabe
    {
        public int Id { get; }
        
        public string Number { get; }
        
        public string Barcode { get; }
        
        public string PatientShippingNumber { get; }

        public string LaboratoryShippingNumber { get; }
        
        public UpdateDnaOrderCommand(
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

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<UpdateDnaOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
            }
        }

        #endregion
    }
}
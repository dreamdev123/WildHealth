using System;
using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class ShipDnaOrderCommand : IRequest<DnaOrder>, IValidatabe
    {
        public int Id { get; }
        
        public string Barcode { get; }
        
        public string PatientShippingNumber { get; }
        
        public string LaboratoryShippingNumber { get; }
        
        public DateTime Date { get; }
        
        public ShipDnaOrderCommand(
            int id,
            string barcode,
            string patientShippingNumber,
            string laboratoryShippingNumber,
            DateTime date)
        {
            Id = id;
            Barcode = barcode;
            PatientShippingNumber = patientShippingNumber;
            LaboratoryShippingNumber = laboratoryShippingNumber;
            Date = date;
        }
        
        #region validation
        
        private class Validator: AbstractValidator<ShipDnaOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.Barcode).NotNull().NotEmpty();
                RuleFor(x => x.PatientShippingNumber).NotNull().NotEmpty();
                RuleFor(x => x.LaboratoryShippingNumber).NotNull().NotEmpty();
                RuleFor(x => x.Date).NotEmpty();
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
        /// <returns></returns>
        public void Validate() => new Validator().ValidateAndThrow(this);

        #endregion
    }
}
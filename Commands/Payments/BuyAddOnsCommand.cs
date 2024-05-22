using System.Collections.Generic;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Entities.EmployerProducts;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Payments
{
    public class BuyAddOnsCommand : IRequest<IEnumerable<Order>>, IValidatabe
    {
        public Patient Patient { get; }

        public EmployerProduct EmployerProduct { get; }

        public IEnumerable<int> SelectedAddOnIds { get; }

        public int PaymentPriceId { get; }

        public bool BuyRequiredAddOns { get; }

        public int PracticeId { get; }

        /// <summary>
        /// If use SkipPaymentError, the buy addOns process should not throw any exceptions
        /// </summary>
        public bool SkipPaymentError { get; }

        public BuyAddOnsCommand(
            Patient patient,
            EmployerProduct employerProduct,
            IEnumerable<int> selectedAddOnIds,
            int paymentPriceId,
            bool buyRequiredAddOns,
            int practiceId,
            bool skipPaymentError)
        {
            Patient = patient;
            EmployerProduct = employerProduct;
            PaymentPriceId = paymentPriceId;
            SelectedAddOnIds = selectedAddOnIds;
            PracticeId = practiceId;
            BuyRequiredAddOns = buyRequiredAddOns;
            SkipPaymentError = skipPaymentError;
        }

        #region validation

        private class Validator : AbstractValidator<BuyAddOnsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Patient).NotNull();
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.PaymentPriceId).GreaterThan(0);
                RuleForEach(x => x.SelectedAddOnIds).GreaterThan(0);
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

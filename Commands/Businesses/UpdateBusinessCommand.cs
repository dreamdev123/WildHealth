using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Businesses;
using WildHealth.Common.Models.Users;

namespace WildHealth.Application.Commands.Businesses
{
    public class UpdateBusinessCommand: IRequest<BusinessModel>, IValidatabe
    {
        public int Id { get; }

        public string Name { get; }

        public string PhoneNumber { get; }

        public string TaxIdNumber { get; }

        public AddressModel Address { get; }

        public AddressModel BillingAddress { get; }

        public UpdateBusinessCommand(
            int id,
            string name,
            string phoneNumber,
            string taxIdNumber,
            AddressModel address,
            AddressModel billingAddress)
        {
            Id = id;
            Name = name;
            PhoneNumber = phoneNumber;
            TaxIdNumber = taxIdNumber;
            Address = address;
            BillingAddress = billingAddress;
        }

        #region validation

        private class Validator : AbstractValidator<UpdateBusinessCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.Name).NotNull().NotEmpty().MaximumLength(100);
                RuleFor(x => x.PhoneNumber).NotNull().NotEmpty().MaximumLength(100);
                RuleFor(x => x.TaxIdNumber).NotNull().NotEmpty().MaximumLength(100);
                RuleFor(x => x.Address).NotNull().SetValidator(new AddressModel.Validator());
                RuleFor(x => x.BillingAddress).NotNull().SetValidator(new AddressModel.Validator());
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

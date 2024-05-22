using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Practices;
using WildHealth.Common.Models.Users;

namespace WildHealth.Application.Commands.Practices
{
    public class UpdatePracticeCommand : IRequest<PracticeModel>, IValidatabe
    {
        public int Id { get; }

        public string Name { get; }

        public string Email { get; }

        public string PhoneNumber { get; }

        public string PreferredUrl { get; }

        public AddressModel Address { get; }

        public bool HideAddressOnSignUp { get; set; }

        public UpdatePracticeCommand(
            int id,
            string name,
            string email,
            string phoneNumber,
            string preferredUrl,
            AddressModel address,
            bool hideAddressOnSignUp)
        {
            Id = id;
            Name = name;
            Email = email;
            PhoneNumber = phoneNumber;
            PreferredUrl = preferredUrl;
            Address = address;
            HideAddressOnSignUp = hideAddressOnSignUp;
        }

        #region validation

        private class Validator : AbstractValidator<UpdatePracticeCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.Name).NotNull().NotEmpty().MaximumLength(100);
                RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
                RuleFor(x => x.PhoneNumber).NotNull().NotEmpty().MaximumLength(100);
                RuleFor(x => x.Address).NotNull().SetValidator(new AddressModel.Validator());
                RuleFor(x => x.PreferredUrl)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(50)
                    .Matches("^([\\w\\d]+)(\\-[\\w\\d]+)*$")
                    .WithMessage("PreferredUrl has wrong format. Accepts only letters and digits.");
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

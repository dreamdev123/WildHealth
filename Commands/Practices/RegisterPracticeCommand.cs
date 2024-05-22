using System.Collections.Generic;
using FluentValidation;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Practices
{
    public class RegisterPracticeCommand : IRequest<Practice>, IValidatabe
    {
        public int BusinessId { get; }
        
        public int PracticeId { get; }
        
        public string PracticeName { get; }
        
        public int LocationId { get; }
        
        public string LocationName { get; }

        public string Email { get; }

        public AddressModel Address { get; }

        public string ProviderFirstName { get; }

        public string ProviderLastName { get; }

        public string ProviderPhoneNumber { get; }

        public string ProviderEmail { get; }

        public string ProviderPassword { get; }
        
        public string ProviderCredentials { get; }

        public IDictionary<string, string> DataTemplates { get; }

        public RegisterPracticeCommand(
            int businessId,
            int practiceId,
            string practiceName,
            int locationId,
            string locationName,
            string email,
            AddressModel address,
            string providerFirstName,
            string providerLastName,
            string providerPhoneNumber,
            string providerEmail,
            string providerPassword,
            string providerCredentials,
            IDictionary<string, string> dataTemplates)
        {
            BusinessId = businessId;
            PracticeId = practiceId;
            PracticeName = practiceName;
            LocationId = locationId;
            LocationName = locationName;
            Email = email;
            Address = address;
            ProviderFirstName = providerFirstName;
            ProviderLastName = providerLastName;
            ProviderPhoneNumber = providerPhoneNumber;
            ProviderEmail = providerEmail;
            ProviderPassword = providerPassword;
            ProviderCredentials = providerCredentials;
            DataTemplates = dataTemplates;
        }
        
        #region validation

        private class Validator : AbstractValidator<RegisterPracticeCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.BusinessId).GreaterThan(0);
                RuleFor(x => x.LocationId).GreaterThan(0);

                RuleFor(x => x.ProviderFirstName).NotNull().NotEmpty();
                RuleFor(x => x.ProviderLastName).NotNull().NotEmpty();
                RuleFor(x => x.ProviderEmail).NotNull().NotEmpty().EmailAddress();
                RuleFor(x => x.ProviderPassword).NotNull().NotEmpty();
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
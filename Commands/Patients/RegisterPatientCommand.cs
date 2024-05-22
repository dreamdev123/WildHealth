using System;
using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Extensions;
using WildHealth.Common.Models.Agreements;
using WildHealth.Common.Models.LeadSources;
using WildHealth.Common.Models.Users;
using WildHealth.Common.Validators;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.User;
using MediatR;
using WildHealth.Common.Models.Patients;

namespace WildHealth.Application.Commands.Patients
{
    /// <summary>
    /// Represents command for patient registration
    /// </summary>
    public class RegisterPatientCommand : IRequest<CreatedPatientModel>, IValidatabe
    {
        public string FirstName { get; }
        public string LastName { get; }
        public Gender Gender { get; }
        public string Email { get; }
        public DateTime Birthday { get; }
        public string PhoneNumber { get; }
        public string Password { get; }
        public AddressModel BillingAddress { get; }
        public AddressModel ShippingAddress { get; }
        public int PracticeId { get; }
        public int? EmployeeId { get; }
        public int? LinkedEmployeeId { get; set; }

        public string PaymentToken { get; }
        public int PaymentPeriodId { get; }
        public int PaymentPriceId { get; }
        public int? FounderId { get; }
        public int[] AddOnIds { get; }
        public ConfirmAgreementModel[] Agreements { get; }
        public PatientLeadSourceModel LeadSource { get; }
        public string InviteCode { get; }
        public bool IsCrossFitAssociated { get; }
        public string EmployerProductKey { get; }
        public string? PromoCode { get; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="gender"></param>
        /// <param name="email"></param>
        /// <param name="birthday"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="password"></param>
        /// <param name="billingAddress"></param>
        /// <param name="shippingAddress"></param>
        /// <param name="practiceId"></param>
        /// <param name="employeeId"></param>
        /// <param name="linkedEmployeeId"></param>
        /// <param name="paymentToken"></param>
        /// <param name="paymentPeriodId"></param>
        /// <param name="paymentPriceId"></param>
        /// <param name="founderId"></param>
        /// <param name="addOnIds"></param>
        /// <param name="agreements"></param>
        /// <param name="isCrossFitAssociated"></param>
        /// <param name="leadSource"></param>
        /// <param name="inviteCode"></param>
        /// <param name="employerProductKey"></param>
        /// /// 
        public RegisterPatientCommand(
            string firstName, 
            string lastName, 
            Gender gender, 
            string email, 
            DateTime birthday, 
            string phoneNumber, 
            string password,
            AddressModel billingAddress, 
            AddressModel shippingAddress, 
            int practiceId,
            int? employeeId,
            int? linkedEmployeeId,
            string paymentToken, 
            int paymentPeriodId, 
            int paymentPriceId, 
            int? founderId,
            int[] addOnIds,
            ConfirmAgreementModel[] agreements,
            PatientLeadSourceModel leadSource,
            string inviteCode,
            bool isCrossFitAssociated, 
            string employerProductKey,
            string? promoCode = null)
        {
            FirstName = firstName;
            LastName = lastName;
            Gender = gender;
            Email = email;
            Birthday = birthday;
            PhoneNumber = phoneNumber;
            Password = password;
            BillingAddress = billingAddress;
            ShippingAddress = shippingAddress;
            PracticeId = practiceId;
            EmployeeId = employeeId;
            LinkedEmployeeId = linkedEmployeeId;
            PaymentToken = paymentToken;
            PaymentPeriodId = paymentPeriodId;
            PaymentPriceId = paymentPriceId;
            FounderId = founderId;
            AddOnIds = addOnIds;
            Agreements = agreements;
            LeadSource = leadSource;
            InviteCode = inviteCode;
            IsCrossFitAssociated = isCrossFitAssociated;
            EmployerProductKey = employerProductKey;
            PromoCode = promoCode;
        }

        #region validation

        private class Validator : AbstractValidator<RegisterPatientCommand>
        {
            public Validator()
            {
                RuleFor(x => x.FirstName).NotEmpty().NotWhitespace();
                RuleFor(x => x.LastName).NotEmpty().NotWhitespace();
                RuleFor(x => x.Email).EmailAddress();
                RuleFor(x => x.PhoneNumber).NotEmpty().NotWhitespace();
                RuleFor(x => x.Password).SetValidator(new PasswordValidator());
                RuleFor(x => x.PaymentToken).NotEmpty().NotWhitespace();
                RuleFor(x => x.BillingAddress).NotNull();
                RuleFor(x => x.ShippingAddress).NotNull();
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.PaymentPriceId).GreaterThan(0);
                RuleFor(x => x.PaymentPeriodId).GreaterThan(0);
                RuleFor(x => x.FounderId).GreaterThan(0).When(x => x.FounderId.HasValue);
                RuleFor(x => x.EmployeeId).GreaterThan(0).When(x => x.EmployeeId.HasValue);
                RuleFor(x => x.LinkedEmployeeId).GreaterThan(0).When(x => x.LinkedEmployeeId.HasValue);
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
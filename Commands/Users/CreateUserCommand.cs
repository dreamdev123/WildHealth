using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Commands.Users
{
    public class CreateUserCommand : IRequest<User>, IValidatabe
    {
        public string? FirstName { get; }
        public string? LastName { get; }
        public string Email { get; }
        public string? PhoneNumber { get; }
        public string Password { get; }
        public DateTime? BirthDate { get; }
        public Gender Gender { get; }
        public UserType UserType { get; }
        public int PracticeId { get; }
        public AddressModel? BillingAddress { get; }
        public AddressModel? ShippingAddress { get; }
        public bool IsRegistrationCompleted { get;  }
        public bool IsVerified { get; }
        public string? Note { get; }
        public bool MarketingSMS { get; }
        
        public bool MeetingRecordingConsent { get; }
        
        public CreateUserCommand(
            string? firstName, 
            string? lastName, 
            string email, 
            string? phoneNumber, 
            string password, 
            DateTime? birthDate, 
            Gender gender,
            UserType userType, 
            int practiceId, 
            AddressModel? billingAddress, 
            AddressModel? shippingAddress,
            bool isVerified,
            bool isRegistrationCompleted,
            bool marketingSms = false,
            bool meetingRecordingConsent = false,
            string? note = null)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            Password = password;
            BirthDate = birthDate;
            Gender = gender;
            UserType = userType;
            PracticeId = practiceId;
            BillingAddress = billingAddress;
            ShippingAddress = shippingAddress;
            IsRegistrationCompleted = isRegistrationCompleted;
            IsVerified = isVerified;
            Note = note;
            MarketingSMS = marketingSms;
            MeetingRecordingConsent = meetingRecordingConsent;
        }
        
        #region Validation
        
        private class Validator : AbstractValidator<CreateUserCommand>
        {
            public Validator()
            {
                // as required in on-boarding process we nee just validate these 2 fields
                RuleFor(x => x.Email).NotNull().NotEmpty();
                RuleFor(x => x.Password).NotNull().NotEmpty();
               
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
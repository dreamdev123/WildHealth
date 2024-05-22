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
    public class UpdateUserCommand : IRequest<User>, IValidatabe
    {
        public int Id { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public DateTime? Birthday { get; }
        
        public Gender Gender { get; }

        public UserType? Type { get; }

        public string Email { get; }

        public string PhoneNumber { get; }

        public AddressModel BillingAddress { get; }
        
        public AddressModel ShippingAddress { get; }
        
        public bool? IsRegistrationCompleted { get; }
        
        public UpdateUserCommand(
            int id, 
            string firstName, 
            string lastName, 
            DateTime? birthday, 
            Gender gender, 
            string email, 
            string phoneNumber, 
            AddressModel billingAddress, 
            AddressModel shippingAddress,
            UserType? userType,
            bool? isRegistrationCompleted = null)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Birthday = birthday;
            Gender = gender;
            Email = email;
            PhoneNumber = phoneNumber;
            BillingAddress = billingAddress;
            ShippingAddress = shippingAddress;
            Type = userType;
            IsRegistrationCompleted = isRegistrationCompleted;
        }
        
        #region Validation
        
        private class Validator : AbstractValidator<UpdateUserCommand>
        {
            public Validator()
            {
                var now = DateTime.UtcNow.Date;
                RuleFor(x => x.FirstName).NotNull().NotEmpty();
                RuleFor(x => x.LastName).NotNull().NotEmpty();
                RuleFor(x => x.BillingAddress).NotNull().NotEmpty();
                RuleFor(x => x.ShippingAddress).NotNull().NotEmpty();
                RuleFor(x => x.Birthday).LessThan(now);
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
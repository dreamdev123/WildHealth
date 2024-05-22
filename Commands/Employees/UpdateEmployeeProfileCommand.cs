using System;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Domain.Entities.Employees;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Employees
{
    public class UpdateEmployeeProfileCommand : IRequest<Employee>, IValidatabe
    {
        public int Id { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public DateTime? Birthday { get; }
        
        public Gender Gender { get; }

        public string Email { get; }

        public string PhoneNumber { get; }

        public string Credentials { get; }

        public string Bio { get; }

        public int[] States { get; }

        public IFormFile ProfilePhoto { get; }

        public AddressModel BillingAddress { get; }

        public AddressModel ShippingAddress { get; }

        public UpdateEmployeeProfileCommand(
            int id, 
            string firstName, 
            string lastName, 
            DateTime? birthday, 
            Gender gender, 
            string email, 
            string phoneNumber,
            string credentials,
            AddressModel billingAddress, 
            AddressModel shippingAddress, 
            string bio, 
            int[] states, 
            IFormFile profilePhoto)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Birthday = birthday;
            Gender = gender;
            Email = email;
            PhoneNumber = phoneNumber;
            Credentials = credentials;
            BillingAddress = billingAddress;
            ShippingAddress = shippingAddress;
            Bio = bio;
            States = states;
            ProfilePhoto = profilePhoto;
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

        private class Validator : AbstractValidator<UpdateEmployeeProfileCommand>
        {
            public Validator()
            {
                RuleFor(x => x.FirstName).NotEmpty();
                RuleFor(x => x.LastName).NotEmpty();
                RuleFor(x => x.Email).NotEmpty().EmailAddress();
                // RuleFor(x => x.BillingAddress).NotNull();
                // RuleFor(x => x.ShippingAddress).NotNull();
                RuleFor(x => x.PhoneNumber).NotNull();
                RuleFor(x => x.Gender).IsInEnum();
                RuleFor(x => x.States).NotNull();
            }
        }

        #endregion
    }
}
using System;
using MediatR;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.Commands.Patients
{
    public class UpdatePatientProfileCommand : IRequest<Patient>
    {
        public int? Id { get; }

        public Guid? IntakeId { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public DateTime? Birthday { get; }
        
        public Gender Gender { get; }

        public string Email { get; }

        public string PhoneNumber { get; }

        public AddressModel BillingAddress { get; }

        public AddressModel ShippingAddress { get; }

        public UpdatePatientProfileCommand(
            int id, 
            string firstName, 
            string lastName, 
            DateTime? birthday, 
            Gender gender, 
            string email, 
            string phoneNumber, 
            AddressModel billingAddress, 
            AddressModel shippingAddress)
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
        }

        public UpdatePatientProfileCommand(
            Guid intakeId, 
            string firstName, 
            string lastName, 
            DateTime? birthday, 
            Gender gender, 
            string email, 
            string phoneNumber, 
            AddressModel billingAddress, 
            AddressModel shippingAddress)
        {
            IntakeId = intakeId;
            FirstName = firstName;
            LastName = lastName;
            Birthday = birthday;
            Gender = gender;
            Email = email;
            PhoneNumber = phoneNumber;
            BillingAddress = billingAddress;
            ShippingAddress = shippingAddress;
        }
    }
}
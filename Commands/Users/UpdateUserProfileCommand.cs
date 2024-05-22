using System;
using MediatR;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Common.Models.Users;

namespace WildHealth.Application.Commands.Users
{
    public class UpdateUserProfileCommand : IRequest<User>
    {
        public string Email { get; }

        public string FirstName { get; }

        public string LastName { get; }
        
        public string PhoneNumber { get; }
        public bool SmsMarketing { get; }
        
        public bool MeetingRecordingConsent { get; }

        public DateTime? Birthdate { get; }
        
        public Gender Gender { get; }
        
        public AddressModel? ShippingAddress { get; }

        public AddressModel? BillingAddress { get; }
        

        public UpdateUserProfileCommand(
            string email,
            string firstName, 
            string lastName, 
            DateTime birthdate, 
            Gender gender,
            string phoneNumber,
            bool smsMarketing,
            bool meetingRecordingConsent,
            AddressModel shippingAddress,
            AddressModel billingAddress,
            bool isVerified = false
            )
        {
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            Birthdate = birthdate;
            Gender = gender;
            PhoneNumber = phoneNumber;
            SmsMarketing = smsMarketing;
            MeetingRecordingConsent = meetingRecordingConsent;
            ShippingAddress = shippingAddress;
            BillingAddress = billingAddress;
        }
    }
}
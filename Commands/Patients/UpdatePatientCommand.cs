﻿using System;
using MediatR;
using WildHealth.Common.Models.LeadSources;
using WildHealth.Common.Models.Patients;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.Commands.Patients
{
    public class UpdatePatientCommand : IRequest<Patient>
    {
        public int Id { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public DateTime? Birthday { get; }

        public Gender Gender { get; }

        public string Email { get; }

        public string PhoneNumber { get; }

        public AddressModel BillingAddress { get; }

        public AddressModel ShippingAddress { get; }

        public PatientOptionsModel Options { get; }

        public int[] EmployeeIds { get; }

        public PatientLeadSourceModel? LeadSource { get; }

        public UpdatePatientCommand(
            int id, 
            string firstName, 
            string lastName, 
            DateTime? birthday, 
            Gender gender, 
            string email, 
            string phoneNumber, 
            AddressModel billingAddress, 
            AddressModel shippingAddress,
            PatientOptionsModel options,
            int[] employeeIds,
            PatientLeadSourceModel? leadSource)
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
            Options = options;
            EmployeeIds = employeeIds;
            LeadSource = leadSource;
        }
    }
}
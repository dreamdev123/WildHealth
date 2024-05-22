using FluentValidation;
using MediatR;
using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.Commands.Patients
{
    /// <summary>
    /// Used for initial patient creation (only with user info)
    /// </summary>
    public class StartPatientRegistrationCommand : IRequest<Patient>, IValidatabe
    {
        public int? FellowId { get; }
        public int PracticeId { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Email { get; }
        public DateTime Birthday { get; }
        public string? PhoneNumber { get; }
        public Gender Gender { get;  }

        public StartPatientRegistrationCommand(
            int? fellowId,
            int practiceId,
            string firstName,
            string lastName,
            string email,
            DateTime birthday,
            string? phoneNumber,
            Gender gender)
            
        {
            FellowId = fellowId;
            PracticeId = practiceId;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Birthday = birthday;
            PhoneNumber = phoneNumber;
            Gender = gender;
        }

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<StartPatientRegistrationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.FirstName).NotNull().NotEmpty();
                RuleFor(x => x.LastName).NotNull().NotEmpty();
                RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
                RuleFor(x => x.Birthday).NotEmpty();
                RuleFor(x => x.Gender).NotEqual(Gender.None);
            }
        }
    }
}

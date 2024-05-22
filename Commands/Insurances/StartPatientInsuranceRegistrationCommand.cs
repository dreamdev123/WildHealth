using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.User;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Insurances
{
    public class StartPatientInsuranceRegistrationCommand : IRequest<Patient>, IValidatabe
    {
        public int UserId { get; }
        
        public string FirstName { get; }
        
        public string LastName { get; }
        
        public DateTime Birthday { get; }
        
        public Gender Gender { get; }
        
        public string PhoneNumber { get; }
        
        public int PracticeId { get; }

        public StartPatientInsuranceRegistrationCommand(
            int userId,
            string firstName,
            string lastName, 
            DateTime birthday,
            Gender gender,
            string phoneNumber,
            int practiceId)
        {
            UserId = userId;
            FirstName = firstName;
            LastName = lastName;
            Birthday = birthday;
            Gender = gender;
            PhoneNumber = phoneNumber;
            PracticeId = practiceId;
        }
        
        #region validation

        private class Validator : AbstractValidator<StartPatientInsuranceRegistrationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.UserId).GreaterThan(0);
                RuleFor(x => x.FirstName).NotNull().NotEmpty();
                RuleFor(x => x.LastName).NotNull().NotEmpty();
                RuleFor(x => x.Birthday).LessThan(DateTime.UtcNow.Date);
                RuleFor(x => x.PhoneNumber).NotNull().NotEmpty();
                RuleFor(x => x.PracticeId).GreaterThan(0);
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
        /// <returns></returns>
        public void Validate() => new Validator().ValidateAndThrow(this);


        #endregion
    }
}
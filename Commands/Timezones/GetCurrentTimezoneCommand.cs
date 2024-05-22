using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Timezones
{
    public class GetCurrentTimezoneCommand : IRequest<TimeZoneInfo>, IValidatabe
    {
        public int? PatientId { get; protected set; }
        
        public int? EmployeeId { get; protected set; }
        
        public int PracticeId { get; protected set; }

        protected GetCurrentTimezoneCommand() {  }

        public static GetCurrentTimezoneCommand ForPatient(int patientId, int practiceId)
        {
            return new GetCurrentTimezoneCommand
            {
                PatientId = patientId,
                PracticeId = practiceId
            };
        }
        
        public static GetCurrentTimezoneCommand ForEmployee(int employeeId, int practiceId)
        {
            return new GetCurrentTimezoneCommand
            {
                EmployeeId = employeeId,
                PracticeId = practiceId
            };
        }
        
        #region validation

        private class Validator : AbstractValidator<GetCurrentTimezoneCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                
                RuleFor(x => x.EmployeeId)
                    .NotNull()
                    .GreaterThan(0)
                    .When(x => x.EmployeeId.HasValue);
                
                RuleFor(x => x.PatientId)
                    .NotNull()
                    .GreaterThan(0)
                    .When(x => x.PatientId.HasValue);
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
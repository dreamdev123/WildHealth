using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Timezones
{
    public class SetPatientTimezoneCommand : IRequest, IValidatabe
    {
        public int PatientId { get; }

        public string Timezone { get; }

        public SetPatientTimezoneCommand(int patientId, string timezone)
        {
            PatientId = patientId;
            Timezone = timezone;
        }
        
        #region validation

        private class Validator : AbstractValidator<SetPatientTimezoneCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);

                RuleFor(x => x.Timezone).NotNull().NotEmpty();
            }
        }

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new SetPatientTimezoneCommand.Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new SetPatientTimezoneCommand.Validator().ValidateAndThrow(this);

        #endregion
    }
}
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Commands.Patients
{
    public class SendPracticumPatientInvitationEmailCommand: IRequest, IValidatabe
    {
        public Patient Patient { get; }

        public int FellowId { get; }

        public SendPracticumPatientInvitationEmailCommand(Patient patient, int fellowId)
        {
            Patient = patient;
            FellowId = fellowId;
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

        private class Validator : AbstractValidator<SendPracticumPatientInvitationEmailCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Patient).NotNull();
                RuleFor(x => x.FellowId).GreaterThan(0);
            }
        }

        #endregion
    }
}

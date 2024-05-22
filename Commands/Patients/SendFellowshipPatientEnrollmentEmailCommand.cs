using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Patients
{
    public class SendFellowshipPatientEnrollmentNotificationCommand: IRequest, IValidatabe
    {
        public int PracticeId { get; }

        public int LocationId { get; }

        public SendFellowshipPatientEnrollmentNotificationCommand(int practiceId, int locationid)
        {
            PracticeId = practiceId;
            LocationId = locationid;
        }

        #region validation

        private class Validator : AbstractValidator<SendFellowshipPatientEnrollmentNotificationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.LocationId).GreaterThan(0);
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

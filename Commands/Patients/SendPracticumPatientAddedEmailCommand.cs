using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Patients
{
    public class SendPracticumPatientAddedNotificationCommand : IRequest, IValidatabe
    {
        public int PracticeId { get; }

        public int LocationId { get; }

        public SendPracticumPatientAddedNotificationCommand(int practiceId, int locationId)
        {
            PracticeId = practiceId;
            LocationId = locationId;
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

        private class Validator : AbstractValidator<SendPracticumPatientAddedNotificationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.LocationId).GreaterThan(0);
            }
        }

        #endregion
    }
}

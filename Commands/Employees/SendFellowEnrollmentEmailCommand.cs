using MediatR;
using FluentValidation;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Employees
{
    public class SendFellowEnrollmentNotificationCommand: IRequest, IValidatabe
    {
        public int PracticeId { get; }

        public int LocationId { get; }

        public SendFellowEnrollmentNotificationCommand(int practiceId, int locationId)
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

        private class Validator : AbstractValidator<SendFellowEnrollmentNotificationCommand>
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

using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Patients
{
    public class SendNewEnrollmentNotificationCommand : IRequest, IValidatabe
    {
        public int PracticeId { get; }
        
        public int LocationId { get; }

        public int PatientId { get; }
        
        public int SubscriptionId { get; }

        public SendNewEnrollmentNotificationCommand(
            int practiceId, 
            int locationId,
            int patientId,
            int subscriptionId)
        {
            PracticeId = practiceId;
            LocationId = locationId;
            PatientId = patientId;
            SubscriptionId = subscriptionId;
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

        private class Validator : AbstractValidator<SendNewEnrollmentNotificationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.LocationId).GreaterThan(0);
                RuleFor(x => x.PracticeId).GreaterThan(0);
            }
        }

        #endregion
    }
}
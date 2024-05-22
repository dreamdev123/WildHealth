using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class CreatePatientSupportConversationCommand : IRequest<Conversation>, IValidatabe
    {
        public int PracticeId { get; }

        public int LocationId { get; }

        public int PatientId { get; }

        public string Subject { get; }

        public CreatePatientSupportConversationCommand(
            int practiceId,
            int locationId,
            int patientId,
            string subject)
        {
            PracticeId = practiceId;
            LocationId = locationId;
            PatientId = patientId;
            Subject = subject;
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

        private class Validator : AbstractValidator<CreatePatientSupportConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.LocationId).GreaterThan(0);
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.Subject).NotNull().NotEmpty();
            }
        }

        #endregion
    }
}
using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class StartHealthCareConversationCommand : IRequest<Conversation?>, IValidatabe
    {
        public int EmployeeId { get; }

        public int PatientId { get; }

        public int LocationId { get; }

        public int PracticeId { get; }

        public StartHealthCareConversationCommand(
            int employeeId,
            int patientId,
            int locationId,
            int practiceId)
        {
            EmployeeId = employeeId;
            PatientId = patientId;
            LocationId = locationId;
            PracticeId = practiceId;
        }

        #region validation

        private class Validator : AbstractValidator<StartHealthCareConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeId).GreaterThan(-1);
                RuleFor(x => x.PatientId).GreaterThan(0);
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

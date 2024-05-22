using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class StartInternalConversationCommand : IRequest<Conversation>, IValidatabe
    {
        public int EmployeeId { get; }

        public int[] EmployeeIds { get; }

        public int PatientId { get; }

        public int LocationId { get; }

        public int PracticeId { get; }

        public StartInternalConversationCommand(
            int employeeId,
            int[] employeeIds,
            int patientId,
            int locationId,
            int practiceId)
        {
            EmployeeId = employeeId;
            EmployeeIds = employeeIds;
            PatientId = patientId;
            LocationId = locationId;
            PracticeId = practiceId;
        }

        #region validation

        private class Validator : AbstractValidator<StartInternalConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeId).GreaterThan(0);
                RuleFor(x => x.EmployeeIds).NotEmpty();
                RuleForEach(x => x.EmployeeIds).ChildRules(x => x.RuleFor(x => x).GreaterThan(0));
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

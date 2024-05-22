using FluentValidation;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class CreatePatientHealthCareConversationCommand : IRequest<Conversation>, IValidatabe
    {
        public int PracticeId { get; }
        public int LocationId { get; }
        public int PatientId { get; }
        public Employee[] ActiveEmployees { get; }
        public Employee[] InactiveEmployees { get; }
        public (Employee employee, Employee delegatedBy)[] DelegatedEmployees { get; }

        public CreatePatientHealthCareConversationCommand(
            int practiceId,
            int locationId,
            int patientId,
            Employee[] activeEmployees,
            Employee[] inactiveEmployees,
            (Employee employee, Employee delegatedBy)[] delegatedEmployees)
        {
            PracticeId = practiceId;
            LocationId = locationId;
            PatientId = patientId;
            ActiveEmployees = activeEmployees;
            InactiveEmployees = inactiveEmployees;
            DelegatedEmployees = delegatedEmployees;
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

        private class Validator : AbstractValidator<CreatePatientHealthCareConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.LocationId).GreaterThan(0);
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.ActiveEmployees)
                    .NotNull()
                    .NotEmpty()
                    .ForEach(x => x.NotNull());
                
                RuleFor(x => x.InactiveEmployees)
                    .NotNull()
                    .ForEach(x => x.NotNull());
                
                RuleFor(x => x.DelegatedEmployees)
                    .NotNull()
                    .ForEach(x => x.NotNull());
            }
        }

        #endregion
    }
}

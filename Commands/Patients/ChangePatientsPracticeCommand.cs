using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Patients
{
    public class ChangePatientsPracticeCommand : IRequest<Patient>, IValidatabe
    {
        public int PatientId { get; }

        public int NewPracticeId { get; }

        public ChangePatientsPracticeCommand(int patientId, int newPracticeId)
        {
            PatientId = patientId;
            NewPracticeId = newPracticeId;
        }
        
        #region validation

        private class Validator : AbstractValidator<ChangePatientsPracticeCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.NewPracticeId).GreaterThan(0);
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
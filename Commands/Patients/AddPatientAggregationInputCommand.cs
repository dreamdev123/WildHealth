using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Commands.Patients
{
    public class AddPatientAggregationInputCommand : IRequest,IValidatabe
    {
        public int PatientId { get; protected set; }

        public AddPatientAggregationInputCommand(int patientId)
        {
            PatientId = patientId;
        }

        #region validation

        private class Validator : AbstractValidator<AddPatientAggregationInputCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
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
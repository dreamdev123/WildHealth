using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Appointments;
using FluentValidation;
using MediatR;
using WildHealth.Domain.Enums.Patient;

namespace WildHealth.Application.Commands.Appointments
{
    /// <summary>
    /// Returns the PatientType
    /// </summary>
    public class PatientTypeResolverCommand : IRequest<PatientType>, IValidatabe
    {
        public int PatientId { get; }

        public PatientTypeResolverCommand(
            int patientId)
        {
            PatientId = patientId;
        }

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<PatientTypeResolverCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
            }
        }
    }
}
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.HealthSummaries;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.HealthSummaries
{
    public class UpdateHealthSummaryCommand : IRequest<HealthSummaryValue>, IValidatabe
    {
        public int PatientId { get; }

        public string Key { get; }

        public string Name { get; }

        public string Value { get; }

        public UpdateHealthSummaryCommand(
            int patientId, 
            string key, 
            string name,
            string value)
        {
            PatientId = patientId;
            Key = key;
            Value = value;
            Name = name;
        }

        #region Validation

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<UpdateHealthSummaryCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Key).NotNull().NotEmpty();
                RuleFor(x => x.Value).NotNull();
                RuleFor(x => x.PatientId).GreaterThan(0);
            }
        }

        #endregion
    }
}
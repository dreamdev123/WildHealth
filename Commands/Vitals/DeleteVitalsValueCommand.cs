using FluentValidation;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Vitals;

namespace WildHealth.Application.Commands.Vitals
{
    public class DeleteVitalsValueCommand : IRequest<ICollection<VitalValue>>, IValidatabe
    {
        public DeleteVitalsValueCommand(int[] vitalsValuesIds, int patientId)
        {
            VitalsValuesIds = vitalsValuesIds;
            PatientId = patientId;
        }

        public int[] VitalsValuesIds { get; }

        public int PatientId { get; }

        #region Validation

        /// <inheritdoc/>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <inheritdoc/>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<DeleteVitalsValueCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.VitalsValuesIds)
                    .Must(x => x.All(i => i > 0))
                    .WithMessage("Each Id must be greater then zero.")
                    .NotNull()
                    .NotEmpty();
            }
        }

        #endregion
    }
}

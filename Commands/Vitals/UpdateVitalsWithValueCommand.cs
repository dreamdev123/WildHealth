using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Vitals;
using WildHealth.Domain.Entities.Vitals;

namespace WildHealth.Application.Commands.Vitals
{
    public class UpdateVitalsWithValueCommand : IRequest<ICollection<VitalValue>>, IValidatabe
    {
        public UpdateVitalsWithValueCommand(ICollection<UpdateVitalValueModel> vitals, int patientId)
        {
            Vitals = vitals;
            PatientId = patientId;
        }

        public ICollection<UpdateVitalValueModel> Vitals { get; }

        public int PatientId { get; }

        /// <inheritdoc/>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <inheritdoc/>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<UpdateVitalsWithValueCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.Vitals)
                    .NotNull()
                    .NotEmpty();
                RuleForEach(x => x.Vitals)
                    .Must(x => x.ValueId > 0 && x.DateTime > DateTime.MinValue);
            }
        }
    }
}

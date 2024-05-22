using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Vitals;
using WildHealth.Domain.Enums.Vitals;

namespace WildHealth.Application.Commands.Vitals
{
    public class CreateVitalDataSetCommand : IRequest<ICollection<Vital>>, IValidatabe
    {
        public CreateVitalDataSetCommand(DateTime dateTime, int patientId, VitalValueSourceType sourceType)
        {
            DateTime = dateTime;
            PatientId = patientId;
            SourceType = sourceType;
        }

        public DateTime DateTime { get; }

        public int PatientId { get; }

        public VitalValueSourceType SourceType { get; }

        /// <inheritdoc/>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <inheritdoc/>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<CreateVitalDataSetCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.DateTime).NotEmpty();
                RuleFor(x => x.SourceType).IsInEnum();
            }
        }
    }
}

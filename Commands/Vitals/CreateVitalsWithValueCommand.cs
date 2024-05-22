using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Vitals;
using WildHealth.Domain.Entities.Vitals;

namespace WildHealth.Application.Commands.Vitals
{
    public class CreateVitalsWithValueCommand : IRequest<ICollection<Vital>>, IValidatabe
    {
        public CreateVitalsWithValueCommand(ICollection<CreateVitalModel> vitaModels, int patientId)
        {
            Vitals = vitaModels;
            PatientId = patientId;
        }

        public ICollection<CreateVitalModel> Vitals { get; }

        public int PatientId { get; }

        /// <inheritdoc/>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <inheritdoc/>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<CreateVitalsWithValueCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.Vitals)
                    .NotNull()
                    .NotEmpty();
                RuleForEach(x => x.Vitals)
                    .Must(x => !string.IsNullOrEmpty(x.Name) && x.DateTime > DateTime.MinValue);
            }
        }
    }
}

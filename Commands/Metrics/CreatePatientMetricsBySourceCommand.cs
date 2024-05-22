using System;
using System.Collections.Generic;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.Commands.Metrics
{
    public class CreatePatientMetricsBySourceCommand : IRequest<List<PatientMetric>>, IValidatabe
    {
        public int PatientId { get; }
        public MetricSource[] Sources { get; }

        public CreatePatientMetricsBySourceCommand(
            int patientId,
            MetricSource[] sources
        )   
        {
            PatientId = patientId;
            Sources = sources;
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

        private class Validator : AbstractValidator<CreatePatientMetricsBySourceCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.Sources).NotNull().NotEmpty();
            }
        }

        #endregion
    }
}
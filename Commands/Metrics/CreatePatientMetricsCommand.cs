using System;
using System.Collections.Generic;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.Commands.Metrics
{
    public class CreatePatientMetricsCommand : IRequest<List<PatientMetric>>, IValidatabe
    {
        public int PatientId { get; }
        public Metric[] Metrics { get; }

        public CreatePatientMetricsCommand(
            int patientId,
            Metric[] metrics
        )   
        {
            PatientId = patientId;
            Metrics = metrics;
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

        private class Validator : AbstractValidator<CreatePatientMetricsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.Metrics).NotNull().NotEmpty();
            }
        }

        #endregion
    }
}
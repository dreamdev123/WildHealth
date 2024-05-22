using System;
using System.Collections.Generic;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Metrics;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.Commands.Metrics
{
    public class GetPatientMetricsCommand : IRequest<IList<PatientMetric>>, IValidatabe
    {
        public int PatientId { get; }
        public GetPatientMetricsRequestModel RequestModel { get; }

        public GetPatientMetricsCommand(
            int patientId,
            GetPatientMetricsRequestModel requestModel
        )   
        {
            PatientId = patientId;
            RequestModel = requestModel;
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

        private class Validator : AbstractValidator<GetPatientMetricsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
            }
        }

        #endregion
    }
}
using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Reports;
using WildHealth.Domain.Entities.Reports;
using MediatR;

namespace WildHealth.Application.Commands.HealthReports
{
    public class RegenerateLatestHealthReportCommand : IRequest<HealthReport>, IValidatabe
    {
        public int PatientId { get; }

        public ReportRecommendationModel[] Recommendations { get; }

        public RegenerateLatestHealthReportCommand(int patientId, ReportRecommendationModel[] recommendations)
        {
            PatientId = patientId;
            Recommendations = recommendations;
        }

        #region private
        
        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        
        private class Validator : AbstractValidator<RegenerateLatestHealthReportCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.Recommendations).NotNull();
            }
        }
        
        #endregion
    }
}
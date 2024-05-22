using FluentValidation;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Common.Models.Reports;
using WildHealth.Application.Commands._Base;
using MediatR;


namespace WildHealth.Application.Commands.HealthReports
{
    public class GenerateNewHealthReportCommand : IRequest<HealthReport>, IValidatabe
    {
        public int PatientId { get; }
        
        public bool PrefillReport { get; }

        public ReportRecommendationModel[] Recommendations { get; }
        
        public GenerateNewHealthReportCommand(int patientId, bool prefillReport, ReportRecommendationModel[] recommendations)
        {
            PatientId = patientId;
            PrefillReport = prefillReport;
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

        
        private class Validator : AbstractValidator<GenerateNewHealthReportCommand>
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
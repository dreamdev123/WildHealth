using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Reports;
using MediatR;

namespace WildHealth.Application.Commands.HealthReports
{
    public class UpdateHealthReportReviewerCommand : IRequest<HealthReport>, IValidatabe
    {
        public int Id { get; }
        
        public int PatientId { get; }
        
        public int ReviewerId { get; }

        public UpdateHealthReportReviewerCommand(
            int id, 
            int patientId,
            int reviewerId)
        {
            Id = id;
            PatientId = patientId;
            ReviewerId = reviewerId;
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

        private class Validator : AbstractValidator<UpdateHealthReportReviewerCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.ReviewerId).GreaterThan(0);
            }
        }
        
        #endregion
    }
}
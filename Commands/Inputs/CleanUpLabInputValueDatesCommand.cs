using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Inputs
{
    public class CleanUpLabInputValueDatesCommand : IRequest, IValidatabe
    {
        public int PatientId { get; }
        
        public int ReportId { get; }
        
        public CleanUpLabInputValueDatesCommand(int patientId, int reportId)
        {
            PatientId = patientId;
            ReportId = reportId;
        }

        #region validation

        private class Validator : AbstractValidator<CleanUpLabInputValueDatesCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.ReportId).GreaterThan(0);
            }
        }

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        #endregion
    }
}
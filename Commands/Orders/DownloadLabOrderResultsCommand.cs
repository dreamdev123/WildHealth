using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class DownloadLabOrderResultsCommand : IRequest<(byte[], string)>, IValidatabe
    {
        public int PatientId { get; }
        
        public int ReportId { get; }
        
        public DownloadLabOrderResultsCommand(int patientId, int reportId)
        {
            PatientId = patientId;
            ReportId = reportId;
        }
        
        #region validation

        private class Validator : AbstractValidator<DownloadLabOrderResultsCommand>
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
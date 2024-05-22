using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Inputs
{
    public class CorrectLabResultsCommand : IRequest, IValidatabe
    {
        public int PatientId { get; }
        
        public int ReportId { get; }
        
        public string OrderNumber { get; }
        
        public CorrectLabResultsCommand(int patientId, int reportId, string orderNumber)
        {
            PatientId = patientId;
            ReportId = reportId;
            OrderNumber = orderNumber;
        }

        #region validation

        private class Validator : AbstractValidator<CorrectLabResultsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.ReportId).GreaterThan(0);
                RuleFor(x => x.OrderNumber).NotNull().NotEmpty();
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
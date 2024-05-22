using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Entities.Reports.Alpha;

namespace WildHealth.Application.Commands.Patients
{
    public class UpdatePatientApoeHealthSummaryCommand: IRequest, IValidatabe
    {

        public int PatientId { get; }
        
        public HealthReport Report { get; }
        
        public UpdatePatientApoeHealthSummaryCommand(int patientId, HealthReport report)
        {
            PatientId = patientId;
            Report = report;
            
        }

        private class Validator : AbstractValidator<UpdatePatientApoeHealthSummaryCommand>
        {
            public Validator()
            {
#pragma warning disable CS0618
                RuleFor(x => x.PatientId).Cascade(CascadeMode.StopOnFirstFailure).GreaterThan(0);
#pragma warning restore CS0618
                RuleFor(x => x.Report).NotNull();
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

    }
}
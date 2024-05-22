using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Enums.Patients;
using WildHealth.Common.Models.Patients;

namespace WildHealth.Application.Events.Patients
{
    public record PatientReassignedEvent(
        PatientReassignmentType PatientReassignmentType,
        int PatientId,
        NewStaffSummaryModel[] SummaryModels) : INotification, IValidatabe
    {
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<PatientReassignedEvent>
        {
            public Validator()
            {
                RuleFor(x => x.SummaryModels.Length > 0);
            }
        }
    }
}
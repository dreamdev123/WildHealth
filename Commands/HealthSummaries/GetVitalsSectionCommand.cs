using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.HealthSummaries;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.HealthSummaries;

public class GetVitalsSectionCommand : IRequest<HealthSummaryValueModel[]>, IValidatabe
{
    public int PatientId { get; }

    public GetVitalsSectionCommand(int patientId)
    {
        PatientId = patientId;
    }
    
    #region Validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetVitalsSectionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
        }
    }

    #endregion
}
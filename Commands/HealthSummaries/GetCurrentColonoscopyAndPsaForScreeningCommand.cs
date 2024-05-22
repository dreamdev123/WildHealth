using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.HealthSummaries;

namespace WildHealth.Application.Commands.HealthSummaries;

public class GetCurrentColonoscopyAndPsaForScreeningCommand: IRequest<(HealthSummaryValueModel,HealthSummaryValueModel)>, IValidatabe
{
    public int PatientId { get; set; }

    public GetCurrentColonoscopyAndPsaForScreeningCommand(int patientId)
    {
        PatientId = patientId;
    } 
    
    #region Validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetCurrentColonoscopyAndPsaForScreeningCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
        }
    }

    #endregion
}
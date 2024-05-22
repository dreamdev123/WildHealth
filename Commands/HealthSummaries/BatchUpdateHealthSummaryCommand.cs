using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.HealthSummaries;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.HealthSummaries;

public class BatchUpdateHealthSummaryCommand : IRequest, IValidatabe
{
    public HealthSummaryValueModel[] Values { get; }

    public BatchUpdateHealthSummaryCommand(HealthSummaryValueModel[] values)
    {
        Values = values;
    }
    
    #region Validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<BatchUpdateHealthSummaryCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Values).NotNull();
        }
    }

    #endregion
}
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.HealthCoachEngagement;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.PatientEngagements;

public record CompleteHealthCoachEngagementCommand(int EngagementTaskId, int UserId) : IRequest<HealthCoachEngagementTaskModel>, IValidatabe
{
    #region validation

    private class Validator : AbstractValidator<CompleteHealthCoachEngagementCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EngagementTaskId).GreaterThan(0);
            RuleFor(x => x.UserId).GreaterThan(0);
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

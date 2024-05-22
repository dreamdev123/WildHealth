using System.Collections.Generic;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Goals;

namespace WildHealth.Application.Commands.Goals;

public class GetPatientGoalsCommand : IRequest<List<GoalModel>>, IValidatabe
{
    public int PatientId { get; }
    public bool IncludeCompleted { get; set; }

    public GetPatientGoalsCommand(int patientId, bool includeCompleted = false)
    {
        PatientId = patientId;
        IncludeCompleted = includeCompleted;
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

    private class Validator : AbstractValidator<GetPatientGoalsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
        }
    }

    #endregion
}

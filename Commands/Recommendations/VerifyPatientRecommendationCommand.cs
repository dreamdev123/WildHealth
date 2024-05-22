using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Recommendations;

namespace WildHealth.Application.Commands.Recommendations;

public class VerifyPatientRecommendationCommand : IRequest<PatientRecommendation>, IValidatabe
{
    public int PatientRecommendationId { get; }

    public VerifyPatientRecommendationCommand(int patientRecommendationId)
    {
        PatientRecommendationId = patientRecommendationId;
    }

     #region Validation
    
    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    /// <returns></returns>
    public void Validate() => new Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<VerifyPatientRecommendationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientRecommendationId).NotNull().GreaterThan(0);
        }
    }
    
    #endregion
}

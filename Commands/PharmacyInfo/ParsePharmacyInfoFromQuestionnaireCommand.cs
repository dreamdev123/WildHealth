using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.PharmacyInfo;

public class ParsePharmacyInfoFromQuestionnaireCommand : IRequest<Unit>, IValidatabe
{
    public int PatientId { get; }

    public int QuestionnaireResultId { get; }
    
    public ParsePharmacyInfoFromQuestionnaireCommand(
        int patientId,
        int questionnaireResultId)
    {
        PatientId = patientId;
        QuestionnaireResultId = questionnaireResultId;
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
    
    private class Validator : AbstractValidator<ParsePharmacyInfoFromQuestionnaireCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.QuestionnaireResultId).GreaterThan(0);
        }
    }
    
    #endregion
}
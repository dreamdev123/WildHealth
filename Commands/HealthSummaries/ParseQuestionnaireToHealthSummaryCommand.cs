using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Questionnaires;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.HealthSummaries;

public class ParseQuestionnaireToHealthSummaryCommand : IRequest, IValidatabe
{
    public Patient Patient { get; }

    public QuestionnaireResult Result { get; }

    public ParseQuestionnaireToHealthSummaryCommand(Patient patient, QuestionnaireResult result)
    {
        Patient = patient;
        Result = result;
    }

    #region Validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<ParseQuestionnaireToHealthSummaryCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Patient).NotNull();
            RuleFor(x => x.Result).NotNull();
        }
    }

    #endregion
}
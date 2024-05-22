using System.Linq;
using WildHealth.Application.Extensions.Questionnaire;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Questionnaires;

namespace WildHealth.Application.CommandHandlers.PharmacyInfo.Flows;

public class ParsePharmacyInfoFromQuestionnaireFlow
{
    private static readonly string[] QuestionKeys = 
    {
        QuestionKey.PharmacyName,
        QuestionKey.PharmacyPhone,
        QuestionKey.PharmacyAddress,
        QuestionKey.PharmacyCity,
        QuestionKey.PharmacyZipCode,
        QuestionKey.PharmacyState,
        QuestionKey.PharmacyCountry
    };
    
    private readonly Patient _patient;
    private readonly QuestionnaireResult _result;

    public ParsePharmacyInfoFromQuestionnaireFlow(
        Patient patient, 
        QuestionnaireResult result)
    {
        _patient = patient;
        _result = result;
    }

    public ParsePharmacyInfoFromQuestionnaireFlowResult Execute()
    {
        var answers = _result
            .Answers
            .Where(x => QuestionKeys.Contains(x.Key))
            .ToArray();

        if (!answers.Any())
        {
            return new ParsePharmacyInfoFromQuestionnaireFlowResult(null);
        }

        var pharmacyInfo = new PatientPharmacyInfo(
            patient: _patient, 
            streetAddress: GetAnswer(answers, QuestionKey.PharmacyAddress),
            city: GetAnswer(answers, QuestionKey.PharmacyCity),
            zipCode: GetAnswer(answers, QuestionKey.PharmacyZipCode),
            state: GetAnswer(answers, QuestionKey.PharmacyState),
            country: GetAnswer(answers, QuestionKey.PharmacyCountry),
            name: GetAnswer(answers, QuestionKey.PharmacyName),
            phone: GetAnswer(answers, QuestionKey.PharmacyPhone)
        );

        return new ParsePharmacyInfoFromQuestionnaireFlowResult(pharmacyInfo);
    }
    
    #region private
    
    /// <summary>
    /// Returns answer value by question key
    /// </summary>
    /// <param name="answers"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private string? GetAnswer(Answer[] answers, string key)
    {
        return answers.FirstOrDefault(x => x.Key == key)?.Value;
    }
    
    #endregion
}

public record ParsePharmacyInfoFromQuestionnaireFlowResult(PatientPharmacyInfo? PharmacyInfo);
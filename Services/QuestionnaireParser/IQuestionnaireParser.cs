using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Questionnaires;

namespace WildHealth.Application.Services.QuestionnaireParser
{
    /// <summary>
    /// Provides methods for parsing health questionnaire results
    /// </summary>
    public interface IQuestionnaireParser
    {
        /// <summary>
        /// Parses questionnaire results into general inputs
        /// </summary>
        /// <param name="generalInputs"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        GeneralInputs Parse(GeneralInputs generalInputs, QuestionnaireResult result);
    }
}
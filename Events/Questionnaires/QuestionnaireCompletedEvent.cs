using MediatR;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Events.Questionnaires
{
    public class QuestionnaireCompletedEvent : INotification
    {
        public QuestionnaireResult QuestionnaireResult { get; }
        public Patient Patient { get; }

        public QuestionnaireCompletedEvent(QuestionnaireResult questionnaireResult, Patient patient)
        {
            QuestionnaireResult = questionnaireResult;
            Patient = patient;
        }
    }
}
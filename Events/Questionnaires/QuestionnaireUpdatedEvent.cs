using MediatR;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Events.Questionnaires
{
    public class QuestionnaireUpdatedEvent : INotification
    {
        public QuestionnaireResult QuestionnaireResult { get; }
        public Patient Patient { get; }

        public QuestionnaireUpdatedEvent(QuestionnaireResult questionnaireResult, Patient patient)
        {
            QuestionnaireResult = questionnaireResult;
            Patient = patient;
        }
    }
}
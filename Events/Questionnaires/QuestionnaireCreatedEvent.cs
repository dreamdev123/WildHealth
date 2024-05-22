using MediatR;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Events.Questionnaires
{
    public class QuestionnaireCreatedEvent : INotification
    {
        public QuestionnaireResult QuestionnaireResult { get; }
        public Patient Patient { get; }

        public QuestionnaireCreatedEvent(QuestionnaireResult questionnaireResult, Patient patient)
        {
            QuestionnaireResult = questionnaireResult;
            Patient = patient;
        }
    }
}
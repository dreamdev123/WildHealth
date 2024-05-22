using MediatR;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Questionnaires;

namespace WildHealth.Application.Events.Questionnaires;

public class QuestionnaireStartedEvent : INotification
{
    public QuestionnaireResult QuestionnaireResult { get; }
    public Patient Patient { get; }

    public QuestionnaireStartedEvent(QuestionnaireResult questionnaireResult, Patient patient)
    {
        QuestionnaireResult = questionnaireResult;
        Patient = patient;
    }
}


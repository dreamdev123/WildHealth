using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Questionnaires;
using WildHealth.Domain.Enums.Questionnaires;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.HealthQuestionnaires;
using WildHealth.IntegrationEvents.HealthQuestionnaires.Payloads;

namespace WildHealth.Application.EventHandlers.Questionnaires
{
    public class SendIntegrationEventOnQuestionnaireStartedEvent : INotificationHandler<QuestionnaireStartedEvent>
    {
        private readonly IEventBus _eventBus;

        public SendIntegrationEventOnQuestionnaireStartedEvent()
        {
            _eventBus = EventBusProvider.Get();
        }

        public async Task Handle(QuestionnaireStartedEvent notification, CancellationToken cancellationToken)
        {
            var questionnaireResult = notification.QuestionnaireResult;
            var patient = notification.Patient;

            switch (questionnaireResult.Questionnaire.Type)
            {
                case QuestionnaireType.FollowUpCallForms:
                    await _eventBus.Publish(new HealthQuestionnaireIntegrationEvent(
                        payload: new HealthQuestionnaireStartedPayload(
                            subject: questionnaireResult.Questionnaire.IntegrationName,
                            order: patient.QuestionnaireResults.Count(q =>
                                q.Questionnaire.Type.Equals(questionnaireResult.Questionnaire.Type))
                        ),
                        patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        eventDate: questionnaireResult.CreatedAt), cancellationToken: cancellationToken);
                    break;
                case QuestionnaireType.HealthForms:
                    await _eventBus.Publish(new HealthQuestionnaireIntegrationEvent(
                        payload: new HealthQuestionnaireStartedPayload(
                            subject: questionnaireResult?.Questionnaire.Name,
                            order: patient.QuestionnaireResults.Count(q => q.Questionnaire.Type.Equals(QuestionnaireType.HealthForms))
                        ),
                        patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        eventDate: DateTime.UtcNow), cancellationToken: cancellationToken);  
                    break;
                case QuestionnaireType.HealthLog:
                    break;
                case QuestionnaireType.Initial:
                default:
                    throw new ArgumentException(
                        "Provided questionnaire type does not have an associated integration event");
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Questionnaires;
using WildHealth.Domain.Enums.Questionnaires;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.HealthLogs;
using WildHealth.IntegrationEvents.HealthLogs.Payloads;
using WildHealth.IntegrationEvents.HealthQuestionnaires;
using WildHealth.IntegrationEvents.HealthQuestionnaires.Payloads;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using MediatR;

namespace WildHealth.Application.EventHandlers.Questionnaires
{
    public class SendIntegrationEventOnQuestionnaireCreatedEvent : INotificationHandler<QuestionnaireCreatedEvent>
    {
        private readonly IEventBus _eventBus;

        public SendIntegrationEventOnQuestionnaireCreatedEvent()
        {
            _eventBus = EventBusProvider.Get();
        }

        public async Task Handle (QuestionnaireCreatedEvent notification, CancellationToken cancellationToken)
        {
            var questionnaireResult = notification.QuestionnaireResult;
            var patient = notification.Patient;

            switch(questionnaireResult.Questionnaire.Type)
            {
                case QuestionnaireType.HealthLog:
                    var order = questionnaireResult.SequenceNumber ?? patient.QuestionnaireResults.Count(q => q.Questionnaire.Type.Equals(QuestionnaireType.HealthLog));
                    await _eventBus.Publish(new HeatlthLogIntegrationEvent(
                        payload: new HealthLogStartedPayload(
                            order: order
                        ),
                        patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        eventDate: questionnaireResult.CreatedAt), cancellationToken: cancellationToken);
                    break;
                case QuestionnaireType.FollowUpCallForms:
                    await _eventBus.Publish(new HealthQuestionnaireIntegrationEvent(
                        payload: new HealthQuestionnaireCreatedPayload(
                            subject: questionnaireResult.Questionnaire.IntegrationName,
                            order: patient.QuestionnaireResults.Count(q =>
                                q.Questionnaire.Type == questionnaireResult.Questionnaire.Type)
                        ),
                        patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        eventDate: questionnaireResult.CreatedAt), cancellationToken: cancellationToken);
                    break;
                case QuestionnaireType.HealthForms:
                    await _eventBus.Publish(new HealthQuestionnaireIntegrationEvent(
                        payload: new HealthQuestionnaireCreatedPayload(
                            subject: questionnaireResult?.Questionnaire.Name,
                            order: patient.QuestionnaireResults.Count(q => q.Questionnaire.Type.Equals(QuestionnaireType.HealthForms))
                        ),
                        patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        eventDate: DateTime.UtcNow), cancellationToken: cancellationToken);  
                    break;
                case QuestionnaireType.Initial:
                default:
                    throw new ArgumentException(
                        "Provided questionnaire type does not have an associated integration event");                     
            }
        }
    }
}

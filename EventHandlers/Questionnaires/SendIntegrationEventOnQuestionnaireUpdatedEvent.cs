using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Domain.Enums.Questionnaires;
using WildHealth.Application.Events.Questionnaires;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.HealthLogs;
using WildHealth.IntegrationEvents.HealthLogs.Payloads;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using MediatR;
using WildHealth.IntegrationEvents.HealthQuestionnaires;
using WildHealth.IntegrationEvents.HealthQuestionnaires.Payloads;

namespace WildHealth.Application.EventHandlers.Questionnaires
{
    public class SendIntegrationEventOnQuestionnaireUpdatedEvent : INotificationHandler<QuestionnaireUpdatedEvent>
    {
        // Implement this event handler and the questionnaire completed event handler
        // Both will have logic to determine the type and send appropriate integration event
        // Go to the command handler and publish the created and completed events when appropriate
        private readonly IEventBus _eventBus;

        public SendIntegrationEventOnQuestionnaireUpdatedEvent()
        {
            _eventBus = EventBusProvider.Get();
        }

        public async Task Handle (QuestionnaireUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var questionnaireResult = notification.QuestionnaireResult;
            var patient = notification.Patient;

            switch(questionnaireResult.Questionnaire.Type)
            {
                case QuestionnaireType.HealthLog:
                    var order = questionnaireResult.SequenceNumber ?? patient.QuestionnaireResults.Count(q => q.Questionnaire.Type.Equals(QuestionnaireType.HealthLog));
                    await _eventBus.Publish(new HeatlthLogIntegrationEvent(
                        payload: new HealthLogCompletedPayload(
                            order: order
                        ),
                        patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        eventDate: DateTime.UtcNow), cancellationToken: cancellationToken);
                    break;
                case QuestionnaireType.HealthForms:
                    await _eventBus.Publish(new HealthQuestionnaireIntegrationEvent(
                        payload: new HealthQuestionnaireUpdatedPayload(
                            subject: questionnaireResult?.Questionnaire.Name,
                            order: patient.QuestionnaireResults.Count(q => q.Questionnaire.Type.Equals(QuestionnaireType.HealthForms))
                        ),
                        patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        eventDate: DateTime.UtcNow), cancellationToken: cancellationToken);
                    break;
                case QuestionnaireType.FollowUpCallForms:
                    break;
                case QuestionnaireType.Initial:
                default:
                    throw new ArgumentException(
                        "Provided questionnaire type does not have an associated integration event");
                                              
            }
        }
    }
}
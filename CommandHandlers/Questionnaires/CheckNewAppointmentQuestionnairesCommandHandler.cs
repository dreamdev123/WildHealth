using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Questionnaires;
using WildHealth.Application.Services.Questionnaires;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.HealthQuestionnaires;
using WildHealth.IntegrationEvents.HealthQuestionnaires.Payloads;

namespace WildHealth.Application.CommandHandlers.Questionnaires;

public class CheckNewAppointmentQuestionnairesCommandHandler : IRequestHandler<CheckNewAppointmentQuestionnairesCommand>
{
    private readonly IQuestionnairesService _questionnairesService;
    private readonly IEventBus _eventBus;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CheckNewAppointmentQuestionnairesCommandHandler> _logger;

    public CheckNewAppointmentQuestionnairesCommandHandler(IQuestionnairesService questionnairesService,
        IDateTimeProvider dateTimeProvider,
        ILogger<CheckNewAppointmentQuestionnairesCommandHandler> logger
        )
    {
        _questionnairesService = questionnairesService;
        _eventBus = EventBusProvider.Get();
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task Handle(CheckNewAppointmentQuestionnairesCommand request, CancellationToken cancellationToken)
    {
        var appointmentQuestionnaires = await _questionnairesService.GetNewAppointmentQuestionnairesAsync();
        
        var date = _dateTimeProvider.UtcNow();
        _logger.LogInformation($"CheckNewAppointmentQuestionnairesCommand processing {appointmentQuestionnaires.Count()} items.");
        
        foreach (var appointmentQuestionnaire in appointmentQuestionnaires)
        {
            try
            {
                var patient = appointmentQuestionnaire.Appointment.Patient;

                await _eventBus.Publish(
                    new HealthQuestionnaireIntegrationEvent(
                        payload: new HealthQuestionnaireCreatedPayload(
                            subject: appointmentQuestionnaire.Questionnaire.IntegrationName,
                            order: patient.QuestionnaireResults.Count(q =>
                                q.Questionnaire.Type.Equals(appointmentQuestionnaire.Questionnaire.Type))),
                        patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        eventDate: date),
                    cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                if (appointmentQuestionnaire?.Appointment?.Patient == null)
                {
                    _logger.LogError($"Could not process an AppointmentQuestionnaire: {e}");
                }
                else
                {
                    var patient = appointmentQuestionnaire.Appointment.Patient;
                    _logger.LogWarning($"Failed processing AppointmentQuestionnaire for patient {patient.Id}: {e}");
                }
            }
        }
    }
}
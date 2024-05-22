using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Recommendations.Flows;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Recommendations;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Recommendations;

namespace WildHealth.Application.EventHandlers.Appointments;

public class VerifyPatientRecommendationsOnAppointmentCompletedEvent : INotificationHandler<AppointmentCompletedEvent>
{
    private readonly IPatientRecommendationsService _patientRecommendationsService;
    private readonly MaterializeFlow _materialize;
    private readonly ILogger<VerifyPatientRecommendationsOnAppointmentCompletedEvent> _logger;

    public VerifyPatientRecommendationsOnAppointmentCompletedEvent(
        IPatientRecommendationsService patientRecommendationsService, 
        MaterializeFlow materialize,
        ILogger<VerifyPatientRecommendationsOnAppointmentCompletedEvent> logger)
    {
        _patientRecommendationsService = patientRecommendationsService;
        _materialize = materialize;
        _logger = logger;
    }

    public async Task Handle(AppointmentCompletedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Appointment.WithType != AppointmentWithType.Provider || !notification.Appointment.PatientId.HasValue)
        {
            return;
        }

        var patientId = notification.Appointment.PatientId.Value;

        var patientRecommendations = await _patientRecommendationsService.GetUnverifiedByPatientIdAsync(
            patientId: patientId, 
            verificationMethod: VerificationMethod.AppointmentCompletion);

        foreach (var patientRecommendation in patientRecommendations)
        {
            try {
                await new VerifyPatientRecommendationFlow(patientRecommendation).Materialize(_materialize);
            } 
            catch (Exception e)
            {
                _logger.LogError($"Failed to verify patient recommendation with [Id] = {patientRecommendation.GetId()} on appointment [Id] = {notification.Appointment.GetId()} completed: {e}");
            }
        }
    }
}

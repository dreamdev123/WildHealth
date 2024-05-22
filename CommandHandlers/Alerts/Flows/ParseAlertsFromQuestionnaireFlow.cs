using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Extensions.Questionnaire;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.DashboardAlerts;
using WildHealth.Domain.Entities.Notifications.Abstracts;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.DashboardAlerts;
using WildHealth.IntegrationEvents.DashboardAlerts.Payloads;

namespace WildHealth.Application.CommandHandlers.Alerts.Flows;

public class ParseAlertsFromQuestionnaireFlow : IMaterialisableFlow
{
    public const string Phq2DashboardAlertDescription = "Patient had a score higher than 2 on the PHQ2 survey. Please schedule a call with their provider as soon as possible.";
    
    private static readonly string[] PhqQuestions =
    {
        QuestionKey.Phq21,
        QuestionKey.Phq22
    };
    
    private static readonly IDictionary<string, int> PhqScores = new Dictionary<string, int>
    {
        { string.Empty, 0 },
        { "not at all", 0 },
        { "several days", 1 },
        { "more than half the days", 2 },
        { "nearly every day", 3 }
    };

    private readonly Patient _patient;
    private readonly QuestionnaireResult _result;
    private readonly DateTime _utcNow;
    private readonly string _patientUrl;

    public ParseAlertsFromQuestionnaireFlow(Patient patient,
        QuestionnaireResult result,
        string patientUrl,
        DateTime utcNow)
    {
        _patient = patient;
        _result = result;
        _utcNow = utcNow;
        _patientUrl = patientUrl;
    }

    public MaterialisableFlowResult Execute()
    {
        var alert = GetPhq2Alert();

        return alert!.Added() + GetIntegrationEvents(alert).ToFlowResult() + GetNotifications(alert).ToFlowResult();
    }

    private IEnumerable<BaseIntegrationEvent> GetIntegrationEvents(DashboardAlert? dashboardAlert)
    {
        if (dashboardAlert is not null)
            yield return new DashboardAlertIntegrationEvent(
                new DashboardAlertCreatedPayload(dashboardAlert.Title.ToLower(), _patient.User.UniversalId.ToString()), 
                _utcNow);
    }

    private IEnumerable<IBaseNotification> GetNotifications(DashboardAlert? dashboardAlert)
    {
        if (dashboardAlert is not null)
            yield return new NewAlertNotification(_patient.User.PracticeId, _patientUrl, _utcNow);
    }

    private DashboardAlert? GetPhq2Alert()
    {
        var scores = PhqQuestions.Select(key =>
        {
            var answer = _result.Answers.FirstOrDefault(x => x.Key == key)?.Value ?? string.Empty;

            return PhqScores.ContainsKey(answer.ToLower())
                ? PhqScores[answer.ToLower()]
                : PhqScores[string.Empty];
        });

        if (scores.Sum() >= 2)
        {
            return new DashboardAlert
            {
                Title = "PHQ2",
                Description = Phq2DashboardAlertDescription,
                PatientId = _patient.GetId()
            };
        }

        return null;
    }
}
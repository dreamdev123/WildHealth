using MediatR;
using WildHealth.Application.Services.Integrations;

namespace WildHealth.Application.Events.Patients;

public class HealthScoreUpdatedEvent:INotification
{
    public int PatientId { get; }

    public HealthScoreUpdatedEvent(int patientId)
    {
        PatientId = patientId;
    }

}
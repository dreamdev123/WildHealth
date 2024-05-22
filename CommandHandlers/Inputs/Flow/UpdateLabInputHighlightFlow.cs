using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Patients;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Inputs;
using WildHealth.IntegrationEvents.Inputs.Payloads;

namespace WildHealth.Application.CommandHandlers.Inputs.Flow;

public record UpdateLabInputHighlightFlow(LabInput LabInput, Patient Patient, bool IsActive, DateTime UtcNow): IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        LabInput.SetHighlighted(IsActive);

        return LabInput.Updated() + RaiseEvent();
    }

    private BaseIntegrationEvent RaiseEvent()
    {
        return new InputIntegrationEvent(
            payload: new LabInputHighlightedPayload(
                highlightedAt: UtcNow,
                inputs: new[]
                {
                    new WildHealth.IntegrationEvents.Inputs.Models.LabInput(
                        name: LabInput.Name,
                        value: LabInput.Values.MaxBy(x => x.Date)?.Value ?? 0,
                        updateDate: UtcNow,
                        isHighlighted: IsActive)
                }),
            patient: new PatientMetadataModel(
                id: Patient.GetId(),
                universalId: Patient.UniversalId.ToString()),
            eventDate: UtcNow
        );
    }
}
using System;
using System.Linq;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Actions;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Actions;

namespace WildHealth.Application.Domain.Actions;

public record  CreateCallToActionFlow(
    Patient Patient, 
    ActionType Type, 
    DateTime? ExpiresAt, 
    ActionReactionType[] Reactions, IDictionary<string, string> Data): IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var callToAction = new CallToAction(
            patient: Patient,
            type: Type,
            reactions: Reactions,
            expiresAt: ExpiresAt,
            data: Data.Select(x => new CallToActionData
            {
                Key = x.Key,
                Value = x.Value
            }).ToList()
        );
        
        return callToAction.Added();
    }
}
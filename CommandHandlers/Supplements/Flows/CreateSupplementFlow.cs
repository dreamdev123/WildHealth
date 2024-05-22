using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Supplement;
using WildHealth.Domain.Models.Timeline;

namespace WildHealth.Application.CommandHandlers.Supplements.Flows;

public class CreateSupplementFlow : IMaterialisableFlow
{
    private readonly int _patientId;
    private readonly string _name;
    private readonly string _dosage;
    private readonly string _instructions;
    private readonly string _purchaseLink;
    private readonly DateTime _utcNow;

    public CreateSupplementFlow(int patientId, string name, string dosage, string instructions, string purchaseLink, DateTime utcNow)
    {
        _patientId = patientId;
        _name = name;
        _dosage = dosage;
        _instructions = instructions;
        _purchaseLink = purchaseLink;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        var patientSupplement = new PatientSupplement(_patientId, _name)
        {
            Dosage = _dosage,
            Instructions = _instructions,
            PurchaseLink = _purchaseLink
        };

        var timelineEvent = new SupplementUpdatedTimelineEvent(_patientId, _utcNow, new SupplementUpdatedTimelineEvent.Data(_name, "added"));
                
        return patientSupplement.Added() + timelineEvent.Added();
    }
}
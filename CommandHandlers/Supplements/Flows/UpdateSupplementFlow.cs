using System;
using System.Net;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Supplement;
using WildHealth.Domain.Models.Timeline;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Supplements.Flows;

public class UpdateSupplementFlow : IMaterialisableFlow
{
    private readonly int _id;
    private readonly PatientSupplement _supplement;
    private readonly string _name;
    private readonly string _dosage;
    private readonly string _instructions;
    private readonly string _purchaseLink;
    private readonly DateTime _utcNow;

    public UpdateSupplementFlow(int id, PatientSupplement supplement, string name, string dosage, string instructions, string purchaseLink, DateTime utcNow)
    {
        _id = id;
        _supplement = supplement;
        _name = name;
        _dosage = dosage;
        _instructions = instructions;
        _purchaseLink = purchaseLink;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_supplement is null)
        {
            var exceptionParam = new AppException.ExceptionParameter(nameof(_id), _id);
            throw new AppException(HttpStatusCode.NotFound, "Patient supplement does not exist", exceptionParam);
        }

        _supplement.Name = _name;
        _supplement.Dosage = _dosage;
        _supplement.Instructions = _instructions;
        _supplement.PurchaseLink = _purchaseLink;
                
        var timelineEvent = new SupplementUpdatedTimelineEvent(_supplement.PatientId, _utcNow, new SupplementUpdatedTimelineEvent.Data(_name, "modified"));

        return _supplement.Updated() + timelineEvent.Added();
    }
}
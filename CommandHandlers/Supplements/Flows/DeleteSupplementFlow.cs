using System;
using System.Net;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Supplement;
using WildHealth.Domain.Models.Timeline;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Supplements.Flows;

public class DeleteSupplementFlow : IMaterialisableFlow
{
    private readonly int _id;
    private readonly PatientSupplement _supplement;
    private readonly DateTime _utcNow;

    public DeleteSupplementFlow(int id, PatientSupplement supplement, DateTime utcNow)
    {
        _id = id;
        _supplement = supplement;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_supplement is null)
        {
            var exceptionParam = new AppException.ExceptionParameter(nameof(_id), _id);
            throw new AppException(HttpStatusCode.NotFound, "Patient supplement does not exist", exceptionParam);
        }
                
        var timelineEvent = new SupplementUpdatedTimelineEvent(_supplement.PatientId, _utcNow, new SupplementUpdatedTimelineEvent.Data(_supplement.Name, "removed"));

        return _supplement.Deleted() + timelineEvent.Added();
    }
}
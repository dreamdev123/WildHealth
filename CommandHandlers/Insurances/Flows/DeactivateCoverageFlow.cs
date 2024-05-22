using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Enums.Insurance;

namespace WildHealth.Application.CommandHandlers.Insurances.Flows;

public class DeactivateCoverageFlow : IMaterialisableFlow
{
    private readonly Coverage _coverage;

    public DeactivateCoverageFlow(Coverage coverage)
    {
        _coverage = coverage;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_coverage.Status != CoverageStatus.NotActive)
        {
            _coverage.Deactivate();

            return new MaterialisableFlowResult(_coverage.Updated());
        }

        return new MaterialisableFlowResult(new EntityAction.None());
    }
}
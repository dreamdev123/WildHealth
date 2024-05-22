using System;
using System.Collections.Generic;
using System.Linq;

namespace WildHealth.Application.Functional.Flow;

public class FlowIterator : IMaterialisableFlow
{
    private readonly List<IMaterialisableFlow> _flows = new();

    public MaterialisableFlowResult Execute()
    {
        var previousResult = MaterialisableFlowResult.Empty;
        return _flows
            .Select(flow =>
            {
                if (flow is PassThroughFlowDecorator passThroughFlow) 
                    previousResult = passThroughFlow.Execute(previousResult);
                else
                    previousResult = flow.Execute();

                return previousResult;

            })
            .Aggregate(
                seed: MaterialisableFlowResult.Empty, 
                func: (accumulate, current) => accumulate.Concat(current));
    }

    public FlowIterator Next(IMaterialisableFlow flow)
    {
        _flows.Add(flow);
        return this;
    }
}

public record PassThroughFlowDecorator(Func<MaterialisableFlowResult, IMaterialisableFlow> flowFactory) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute() => throw new NotImplementedException($"Use parametrized version of Execute instead");
    public MaterialisableFlowResult Execute(MaterialisableFlowResult passThroughResult) => flowFactory.Invoke(passThroughResult).Execute();
}
using System;

namespace WildHealth.Application.Functional.Flow;

public class FlowAdapter : IMaterialisableFlow
{
    private readonly Func<MaterialisableFlowResult>? _execute;
    private readonly MaterialisableFlowResult? _result;

    public FlowAdapter(Func<MaterialisableFlowResult> execute)
    {
        _execute = execute;
    }
    
    public FlowAdapter(MaterialisableFlowResult result)
    {
        _result = result;
    }
    
    public MaterialisableFlowResult Execute()
    {
        return _result ?? _execute!.Invoke();
    }
}
using System;

namespace WildHealth.Application.Durable.Chain;

public class ChainStateChangedException : Exception
{
    public ChainStateChangedException(
        string message,
        Exception inner,
        string stepName,
        object payload) : base(message, inner)
    {
        StepName = stepName;
        Payload = payload;
    }

    /// <summary>
    /// Step name where the chain failed at
    /// </summary>
    public string StepName { get; }
    
    /// <summary>
    /// Current chain step payload
    /// </summary>
    public object Payload { get; }
}
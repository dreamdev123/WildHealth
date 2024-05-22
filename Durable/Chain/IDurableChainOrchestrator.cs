using System;
using System.Threading.Tasks;
using WildHealth.Infrastructure.Communication.Messages;

namespace WildHealth.Application.Durable.Chain;

public interface IDurableChainOrchestrator
{
    /// <summary>
    /// Runs the chain of operations in a durable way
    /// </summary>
    /// <param name="payload">The payload that will be passed through the pipeline</param>
    /// <param name="startAtStep">The step name where the chain starts execution at</param>
    /// <param name="chainBuilder">Durable chain setup</param>
    Task<TPayload?> Run<TPayload>(TPayload? payload,
        string startAtStep,
        Action<ChainOfResponsibility<TPayload>> chainBuilder) where TPayload : IEvent;
}
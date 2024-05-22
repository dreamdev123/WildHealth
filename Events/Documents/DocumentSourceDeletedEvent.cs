using System;
using MediatR;

namespace WildHealth.Application.Events.Documents;

public record DocumentSourceDeletedEvent(int DocumentSourceId, Guid[] ChunkUniversalIds) : INotification
{
    
}
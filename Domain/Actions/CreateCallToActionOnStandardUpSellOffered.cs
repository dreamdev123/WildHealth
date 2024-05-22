using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Domain.Enums.Actions;

namespace WildHealth.Application.Domain.Actions;

public record CallToActionSuccessTriggerEvent(
    int PatientId, 
    ActionType Type, 
    DateTime? ExpiresAt, 
    ActionReactionType[] Reactions, 
    IDictionary<string, string> Data) : INotification
{
    
}

public class CreateCallToActionOnSuccessTrigger: INotificationHandler<CallToActionSuccessTriggerEvent>
{
    private readonly IMediator _mediator;

    public CreateCallToActionOnSuccessTrigger(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(CallToActionSuccessTriggerEvent @event, CancellationToken cancellationToken)
    {
        var command = new CreateCallToActionCommand(
            patientId: @event.PatientId,
            type: @event.Type,
            reactions: @event.Reactions,
            expiresAt: @event.ExpiresAt,
            data: @event.Data
        );

        await _mediator.Send(command, cancellationToken);
    }
}
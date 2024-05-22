using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Application.Services.Patients;

namespace WildHealth.Application.EventHandlers.Subscriptions;

public class UnAssignEmployeesOnSubscriptionCancelledEvent : INotificationHandler<SubscriptionCancelledEvent>
{
    private readonly IPatientsService _patientsService;

    public UnAssignEmployeesOnSubscriptionCancelledEvent(IPatientsService patientsService)
    {
        _patientsService = patientsService;
    }

    public async Task Handle(SubscriptionCancelledEvent notification, CancellationToken cancellationToken)
    {
        if (notification.IsRenewal) return;
        await _patientsService.AssignToEmployeesAsync(notification.Patient, Array.Empty<int>());
    }
}
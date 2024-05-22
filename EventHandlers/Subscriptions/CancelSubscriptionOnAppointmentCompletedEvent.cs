using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Models.Appointments;

namespace WildHealth.Application.EventHandlers.Subscriptions;

public class CancelSubscriptionOnAppointmentCompletedEvent : INotificationHandler<AppointmentCompletedEvent>
{
    private const string CancellationReason = "Needs activation";
    
    private readonly ISubscriptionService _subscriptionService;
    private readonly IMediator _mediator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelSubscriptionOnAppointmentCompletedEvent(
        ISubscriptionService subscriptionService, 
        IMediator mediator, 
        IDateTimeProvider dateTimeProvider)
    {
        _subscriptionService = subscriptionService;
        _mediator = mediator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(AppointmentCompletedEvent notification, CancellationToken cancellationToken)
    {
        var appointmentDomain = AppointmentDomain.Create(notification.Appointment);
        
        if (appointmentDomain.IsImc())
        {
            var patientId = notification.Appointment.PatientId;
            
            var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(patientId ?? 0);
            if (subscription.CanBeActivated())
            {
                // Schedule subscription cancellation 48 hours after IMC is completed
                var cancelSubscriptionCommand = new CancelSubscriptionsCommand(
                    id: subscription.GetId(),
                    reasonType: CancellationReasonType.Other,
                    reason: CancellationReason,
                    date: _dateTimeProvider.UtcNow().AddHours(48)
                );
                                            
                await _mediator.Send(cancelSubscriptionCommand, cancellationToken);
            }
        }
    }
}
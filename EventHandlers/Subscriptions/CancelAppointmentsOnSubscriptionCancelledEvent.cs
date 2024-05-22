using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.EventHandlers.Subscriptions;

public class CancelAppointmentsOnSubscriptionCancelledEvent : INotificationHandler<SubscriptionCancelledEvent>
{
    private readonly IAppointmentsService _appointmentsService;
    private readonly IMediator _mediator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CancelAppointmentsOnSubscriptionCancelledEvent> _logger;

    public CancelAppointmentsOnSubscriptionCancelledEvent(
        IAppointmentsService appointmentsService,
        IMediator mediator,
        IDateTimeProvider dateTimeProvider,
        ILogger<CancelAppointmentsOnSubscriptionCancelledEvent> logger)
    {
        _appointmentsService = appointmentsService;
        _mediator = mediator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task Handle(SubscriptionCancelledEvent notification, CancellationToken cancellationToken)
    {
        if (notification.IsRenewal) return;
        var appointments = await _appointmentsService.GetPatientAppointmentsAsync(notification.Patient.GetId(),
            startDate: _dateTimeProvider.UtcNow());
        
        foreach (var appointment in appointments.Where(x => x.Status is not AppointmentStatus.Canceled))
        {
            var result = await _mediator.Send(new CancelAppointmentCommand(appointment.GetId(), 0,
                AppointmentCancellationReason.Cancelled), cancellationToken).ToTry();
            result.DoIfError(ex => _logger.LogWarning(ex,
                "Failed to cancel appointment on subscription cancelled. [AppointmentId]: {AppointmentId}, [SubscriptionId]: {SubscriptionId}",
                appointment.GetId(), notification.Subscription.GetId()));
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.AppointmentReminder;

public record AppointmentCancellationRequestedEvent(int AppointmentId, int UserId) : INotification;

public class AppointmentCancellationRequestedEventHandler : INotificationHandler<AppointmentCancellationRequestedEvent>
{
    private readonly IMediator _mediator;

    public AppointmentCancellationRequestedEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(AppointmentCancellationRequestedEvent notification, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelAppointmentCommand(notification.AppointmentId, notification.UserId, AppointmentCancellationReason.Cancelled, "sms"), cancellationToken);
    }
}
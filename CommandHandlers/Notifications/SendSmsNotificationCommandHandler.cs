using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Notifications;
using WildHealth.Application.Services.Notifications;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Enums;

namespace WildHealth.Application.CommandHandlers.Notifications;

public class SendSmsNotificationCommandHandler : IRequestHandler<SendSmsNotificationCommand>
{
    private readonly IPatientsService _patientsService;
    private readonly INotificationService _notificationService;

    public SendSmsNotificationCommandHandler(IPatientsService patientsService, INotificationService notificationService)
    {
        _patientsService = patientsService;
        _notificationService = notificationService;
    }

    public async Task Handle(SendSmsNotificationCommand request, CancellationToken cancellationToken)
    {
        var patients = await _patientsService.GetPatientsForNotificationAsync(request.PaymentPlans, request.OnlyPatientIds, request.SignupDateFrom, request.SignupDateTo,
            request.HasCompletedAppointment, request.HasActiveSubscription);
        var users = patients.Select(x => x.User);
        
        var smsNotification = new ScheduledSmsNotification(NotificationType.ScheduledSmsNotification, users, request.Text, request.SendAt, request.TextParameters);
        await _notificationService.CreateNotificationAsync(smsNotification);
    }
}
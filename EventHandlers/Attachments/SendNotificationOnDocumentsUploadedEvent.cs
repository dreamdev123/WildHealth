using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Attachments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Notifications;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;

namespace WildHealth.Application.EventHandlers.Attachments;

public class SendNotificationOnDocumentsUploadedEvent : INotificationHandler<DocumentsUploadedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUsersService _usersService;
    private readonly IEmployeeService _employeeService;

    public SendNotificationOnDocumentsUploadedEvent(INotificationService notificationService, IUsersService usersService, IEmployeeService employeeService)
    {
        _notificationService = notificationService;
        _usersService = usersService;
        _employeeService = employeeService;
    }

    public async Task Handle(DocumentsUploadedEvent notification, CancellationToken cancellationToken)
    {
        var patient = notification.Patient;
        if (patient == null)
        {
            return;
        }
        
        var employees = (await _employeeService.GetAssignedToAsync(patient.GetId()))
            .Where(x => x.UserId != notification.UploadedByUserId); //excluding the uploader from notification recipients

        var uploadedByUser = await _usersService.GetAsync(notification.UploadedByUserId);
        foreach (var employee in employees)
        {
            await _notificationService.CreateNotificationAsync(new DocumentsUploadedNotification(patient, employee, notification.Amount, uploadedByUser));
        }
    }
}
﻿using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.CommandHandlers.Employees
{
    public class SendFellowEnrollmentNotificationCommandHandler : IRequestHandler<SendFellowEnrollmentNotificationCommand>
    {
        private readonly PermissionType[] _permissions = { PermissionType.FellowshipNotifications };

        private readonly INotificationService _notificationService;
        private readonly IEmployeeService _employeesService;

        public SendFellowEnrollmentNotificationCommandHandler(
            INotificationService notificationService,
            IEmployeeService employeesService)
        {
            _notificationService = notificationService;
            _employeesService = employeesService;
        }

        public async Task Handle(SendFellowEnrollmentNotificationCommand command, CancellationToken cancellationToken)
        {
            var fellowshipManagers = await _employeesService.GetEmployeesByPermissionsAsync(_permissions, command.PracticeId, command.LocationId);

            var receivers = fellowshipManagers.Select(x => x.User);

            await _notificationService.CreateNotificationAsync(new FellowEnrollmentNotification(receivers));
        }
    }
}

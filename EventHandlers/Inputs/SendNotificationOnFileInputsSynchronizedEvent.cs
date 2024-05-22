using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Inputs;
using WildHealth.Application.Services.Notifications;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using MediatR;
using Microsoft.Extensions.Logging;
using System;

namespace WildHealth.Application.EventHandlers.Inputs
{
    public class SendNotificationOnFileInputsSynchronizedEvent : INotificationHandler<FileInputsSynchronizedEvent>
    {
        private readonly IPatientsService _patientsService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SendNotificationOnFileInputsSynchronizedEvent> _logger;

        public SendNotificationOnFileInputsSynchronizedEvent(
            IPatientsService patientsService,
            INotificationService notificationService,
            ILogger<SendNotificationOnFileInputsSynchronizedEvent> logger)
        {
            _patientsService = patientsService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(FileInputsSynchronizedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var patient = await _patientsService.GetByIdAsync(notification.PatientId);

                if (!patient.GetAssignedEmployees().Any())
                {
                    return;
                }

                await _notificationService.CreateNotificationAsync(
                        new NewResultsNotification(patient, notification.Type));
                
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    $"Send notification event on file inputs synchronized for [PatientId] = {notification.PatientId} has failed",
                    e);
                // ignored
            }
        }
    }
}
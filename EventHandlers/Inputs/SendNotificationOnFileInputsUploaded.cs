using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Inputs;
using WildHealth.Application.Services.Notifications;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using MediatR;
using System;
using Microsoft.Extensions.Logging;

namespace WildHealth.Application.EventHandlers.Inputs
{
    public class SendNotificationOnFileInputsUploaded : INotificationHandler<FileInputsUploadedEvent>
    {
        private readonly IPatientsService _patientsService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SendNotificationOnFileInputsUploaded> _logger;

        public SendNotificationOnFileInputsUploaded(
            IPatientsService patientsService,
            INotificationService notificationService,
            ILogger<SendNotificationOnFileInputsUploaded> logger)
        {
            _patientsService = patientsService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(FileInputsUploadedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var patient = await _patientsService.GetByIdAsync(notification.PatientId);

                if (!patient.GetAssignedEmployees().Any())
                {
                    return;
                }

                await _notificationService.CreateNotificationAsync(new NewResultsNotification(patient, notification.InputType));
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Send notification event on file inputs uploaded for [PatientId] = {notification.PatientId} has failed with [Error]: {e.ToString()}");
                // ignored
            }
        }
    }
}
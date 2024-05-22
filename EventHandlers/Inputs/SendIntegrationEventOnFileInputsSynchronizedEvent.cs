using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Inputs;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.DnaKits;
using WildHealth.IntegrationEvents.DnaKits.Payloads;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using MediatR;
using Microsoft.Extensions.Logging;

namespace WildHealth.Application.EventHandlers.Inputs
{
    public class SendIntegrationEventOnFileInputsSynchronizedEvent : INotificationHandler<FileInputsSynchronizedEvent>
    {
        private readonly IPatientsService _patientsService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<SendIntegrationEventOnFileInputsSynchronizedEvent> _logger;

        public SendIntegrationEventOnFileInputsSynchronizedEvent(IPatientsService patientsService,
            ILogger<SendIntegrationEventOnFileInputsSynchronizedEvent> logger)
        {
            _patientsService = patientsService;
            _eventBus = EventBusProvider.Get();
            _logger = logger;
        }

        public async Task Handle(FileInputsSynchronizedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var patient = await _patientsService.GetByIdAsync(notification.PatientId);

                if (notification.Type != FileInputType.DnaReport)
                {
                    return;
                }

                // set isManualUpload to false since FileInputsSynchronizedEvent comes from automated file loading
                await _eventBus.Publish(new DnaKitIntegrationEvents(
                    new DnaKitUploadedPayload(
                        filePath: notification.FilePath,
                        isManualUpload: false
                    ),
                    new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                    DateTime.UtcNow
                ), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Send integration event on file inputs synchronized for [PatientId] = {notification.PatientId} has failed with [Error]: {e.ToString()}");
                // ignored
            }
        }
    }
}
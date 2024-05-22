using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Inputs;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.DnaKits;
using WildHealth.IntegrationEvents.DnaKits.Payloads;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;

namespace WildHealth.Application.EventHandlers.Inputs
{
    public class SendIntegrationEventOnFileInputsUploaded : INotificationHandler<FileInputsUploadedEvent>
    {
        private readonly IPatientsService _patientsService;
        private readonly IEventBus _eventBus;

        public SendIntegrationEventOnFileInputsUploaded(
            IPatientsService patientsService
            )
        {
            _patientsService = patientsService;
            _eventBus = EventBusProvider.Get();
        }

        public async Task Handle(FileInputsUploadedEvent notification, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(notification.PatientId, PatientSpecifications.PatientUserSpecification);

            if (notification.InputType != FileInputType.DnaReport)
            {
                return;
            }
            
            // set isManualUpload to true since FileInputsUploadedEvent comes from manually loading data files
            await _eventBus.Publish(new DnaKitIntegrationEvents(
                new DnaKitUploadedPayload(
                    filePath: notification.FilePath,
                    isManualUpload: true
                ),
                new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                DateTime.UtcNow
            ), cancellationToken);
        }
    }
}
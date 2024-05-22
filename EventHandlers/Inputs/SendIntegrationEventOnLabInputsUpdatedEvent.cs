using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Inputs;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Inputs;
using IntegrationInputs = WildHealth.IntegrationEvents.Inputs.Models;
using WildHealth.IntegrationEvents.Inputs.Payloads;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Utils.DateTimes;

namespace WildHealth.Application.EventHandlers.Inputs
{
    public class SendIntegrationEventOnLabInputsUpdatedEvent : INotificationHandler<LabInputsUpdatedEvent>
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IPatientsService _patientsService;
        private readonly IEventBus _eventBus;
        private readonly IMapper _mapper;
        private readonly ILogger<SendIntegrationEventOnLabInputsUpdatedEvent> _logger;

        public SendIntegrationEventOnLabInputsUpdatedEvent(
            IDateTimeProvider dateTimeProvider,
            IPatientsService patientsService,
            IMapper mapper,
            ILogger<SendIntegrationEventOnLabInputsUpdatedEvent> logger)
        {
            _dateTimeProvider = dateTimeProvider;
            _patientsService = patientsService;
            _eventBus = EventBusProvider.Get();
            _mapper = mapper;
            _logger = logger;
        }

        public async Task Handle(LabInputsUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var utcNow = _dateTimeProvider.UtcNow();
            
            try
            {
                var patient = await _patientsService.GetByIdAsync(notification.PatientId);

                foreach (var grouping in GroupInputsByLabDate(notification.Inputs))
                {
                    await _eventBus.Publish(new InputIntegrationEvent(
                        payload: new LabInputsUpdatedPayload(
                            labDate: grouping.Key,
                            updatedAt: notification.UpdatedAt,
                            currentLab: grouping.Key.Date == notification.MostRecentLabDate.Date,
                            inputs: MapLabInputs(grouping.ToList())
                        ),
                        patient: new PatientMetadataModel(patient.GetId(), patient.User?.UserId()),
                        eventDate: utcNow
                    ), cancellationToken);
                }

                if (notification.CreatedInputs.Any())
                {
                    await _eventBus.Publish(new InputIntegrationEvent(
                        payload: new LabInputsCreatedPayload(
                            createdAt: utcNow,
                            inputs: notification.CreatedInputs.Select(x => Map(x, utcNow))
                        ),
                        patient: new PatientMetadataModel(patient.GetId(), patient.User?.UserId()),
                        eventDate: utcNow
                    ), cancellationToken);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Send integration event on lab inputs updated for [PatientId] = {notification.PatientId} has failed with [Error]: {e.ToString()}");
                // ignored
            }
        }

        private IEnumerable<IGrouping<DateTime, LabInputValue>> GroupInputsByLabDate(ICollection<LabInputValue> inputs)
        {
            return inputs.GroupBy(x => x.Date.GetValueOrDefault());
        }

        private ICollection<IntegrationInputs.LabInput> MapLabInputs(ICollection<LabInputValue> inputs)
        {
            return inputs.Select(i => _mapper.Map<IntegrationInputs.LabInput>(i)).ToList();
        }

        private IntegrationInputs.LabInput Map(LabInput input, DateTime utcNow)
        {
            return new IntegrationInputs.LabInput(
                name: input.Name,
                value: input.Values.MaxBy(x => x.Date)?.Value ?? 0,
                updateDate: utcNow,
                isHighlighted: input.IsHighlighted()
            );
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Patients;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Patients;
using WildHealth.IntegrationEvents.Patients.Payloads;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.Application.Services.Inputs;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using WildHealth.Application.Services.HealthScore;
using WildHealth.Application.Services.Patients;
using WildHealth.ClarityCore.WebClients.HealthScore;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendIntegrationEventOnPatientUpdatedEvent : INotificationHandler<PatientUpdatedEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly IInputsService _inputsService;
        private readonly IMapper _mapper;
        private readonly IHealthScoreService _healthScoreService;
        private readonly IWebHostEnvironment _environment;
        private readonly IPatientsService _patientsService;

        public SendIntegrationEventOnPatientUpdatedEvent(
            IEventBus eventBus,
            IInputsService inputsService,
            IHealthScoreService healthScoreService,
            IWebHostEnvironment environment,
            IMapper mapper,
            IPatientsService patientsService) 
        {
            _eventBus = eventBus;
            _inputsService = inputsService;
            _mapper = mapper;
            _patientsService = patientsService;
            _healthScoreService = healthScoreService;
            _environment = environment;
        }        

        public async Task Handle(PatientUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(notification.PatientId);
            var user = patient.User;
            
            var subscription = patient.MostRecentSubscription;
            var orderTypes = patient
                .Orders
                .Select(x => x.Type)
                .Distinct()
                .ToArray();
            
            var aggregator = await _inputsService.GetAggregatorAsync(patient.GetId());
            var healthScore = await _healthScoreService.GetPatientHealthScore(patient.GetId().ToString());
            var payload = _mapper.Map<PatientUpdatedPayload>(patient);
            _mapper.Map(user, payload);
            _mapper.Map(subscription, payload); 
            _mapper.Map(orderTypes, payload);
            _mapper.Map(aggregator, payload);
            _mapper.Map(healthScore, payload);
            _mapper.Map(_environment, payload);
            
            payload.HealthScore = healthScore?.PatientScore?.Score?.ToString() ?? string.Empty;

            await _eventBus.Publish(new PatientIntegrationEvent(
                payload: payload,
                patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                practice: new PracticeMetadataModel(patient.User.PracticeId),
                eventDate: DateTime.Now), cancellationToken);
        }
    }
}
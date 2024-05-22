using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Practices;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Patients;
using WildHealth.IntegrationEvents.Patients.Payloads;
using WildHealth.IntegrationEvents.Practices;
using WildHealth.IntegrationEvents.Practices.Payloads;
using WildHealth.Application.Services.Inputs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.HealthScore;
using WildHealth.ClarityCore.Exceptions;
using WildHealth.ClarityCore.Models.HealthScore;
using WildHealth.Domain.Enums.Orders;
using AutoMapper;
using MediatR;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendIntegrationEventOnPatientCreatedEvent : INotificationHandler<PatientCreatedEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly IMapper _mapper;
        private readonly IInputsService _inputsService;
        private readonly IPracticeService _practiceService;
        private readonly IAddOnsService _addOnsService;
        private readonly IHealthScoreService _healthScoreService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SendIntegrationEventOnPatientCreatedEvent> _logger;
        private readonly IPatientsService _patientsService;
        private readonly ISubscriptionService _subscriptionService;

        public SendIntegrationEventOnPatientCreatedEvent(
            IInputsService inputsService, 
            IPracticeService practiceService, 
            IAddOnsService addOnsService,
            IEventBus eventBus,
            IHealthScoreService healthScoreService,
            IWebHostEnvironment environment,
            ILogger<SendIntegrationEventOnPatientCreatedEvent> logger,
            IMapper mapper,
            IPatientsService patientsService,
            ISubscriptionService subscriptionService)
        {
            _eventBus = eventBus;
            _inputsService = inputsService;
            _practiceService = practiceService;
            _addOnsService = addOnsService;
            _mapper = mapper;
            _patientsService = patientsService;
            _subscriptionService = subscriptionService;
            _healthScoreService = healthScoreService;
            _environment = environment;
            _logger = logger;
        }

        public async Task Handle(PatientCreatedEvent notification, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(notification.PatientId, PatientSpecifications.PatientWithAggregationInputs);
            var user = patient.User;
            var subscription = await _subscriptionService.GetAsync(notification.SubscriptionId);
            var addOns = await _addOnsService.GetByIdsAsync(
                notification.SelectedAddOnIds, 
                patient.User.PracticeId);
            
            var aggregator = await _inputsService.GetAggregatorAsync(patient.GetId());
            HealthScoreResponseModel? healthScore = null;
            try
            {
                healthScore = await _healthScoreService.GetPatientHealthScore(patient.GetId().ToString());
            }
            catch (ClarityCoreException err)
            {
                _logger.LogInformation($"Error getting health score from Core for patient [id] {patient.Id}, error : {err.Message}" );
            }

            var payload = _mapper.Map<PatientCreatedPayload>(patient);
            _mapper.Map(user, payload);
            _mapper.Map(subscription, payload);
            _mapper.Map(aggregator, payload);
            _mapper.Map(healthScore, payload);
            _mapper.Map(_environment, payload);
            
            if (addOns.Any())
            {
                payload.DnaKit = addOns.Any(o => OrderType.Dna.Equals(o.OrderType));
                payload.EpiKit = addOns.Any(o => OrderType.Epigenetic.Equals(o.OrderType));
                payload.LabKit = addOns.Any(o => OrderType.Lab.Equals(o.OrderType));
            }

            payload.HealthScore = healthScore?.PatientScore?.Score?.ToString() ?? string.Empty;
            
            await _eventBus.Publish(new PatientIntegrationEvent(
                payload: payload,
                patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                practice: new PracticeMetadataModel(patient.User.PracticeId),
                eventDate: patient.CreatedAt), cancellationToken
            );
            
            // Also check to see if this is the first patient for the practice
            var patientCount = await _practiceService.GetPatientCount(patient.User.PracticeId);

            if (patientCount == 1) {

                var practicePayload = new PracticeFirstPatientRegisteredPayload(patient.User.UniversalId.ToString());

                await _eventBus.Publish(new PracticeIntegrationEvent(
                    payload: practicePayload,
                    practice: new PracticeMetadataModel(patient.User.PracticeId),
                    eventDate: patient.CreatedAt), cancellationToken);

            }
        }
    }
}
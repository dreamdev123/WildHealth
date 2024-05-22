using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Events.Users;
using WildHealth.Application.Services.HealthScore;
using WildHealth.Application.Services.Inputs;
using WildHealth.ClarityCore.Exceptions;
using WildHealth.ClarityCore.Models.HealthScore;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Users;
using WildHealth.IntegrationEvents.Users.Payloads;

namespace WildHealth.Application.EventHandlers.Users
{
    public class SendIntegrationEventOnUserAuthenticatedEvent : INotificationHandler<UserAuthenticatedEvent>
    {
        private readonly IInputsService _inputsService;
        private readonly IHealthScoreService _healthScoreService;
        private readonly IWebHostEnvironment _environment;
        private readonly IEventBus _eventBus;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
      
        public SendIntegrationEventOnUserAuthenticatedEvent(
            IInputsService inputsService,
            IHealthScoreService healthScoreService,
            IWebHostEnvironment environment,
            IMediator mediator, 
            IMapper mapper, 
            ILogger<SendIntegrationEventOnUserAuthenticatedEvent> logger)
        {
            _inputsService = inputsService;
            _healthScoreService = healthScoreService;
            _environment = environment;
            _mediator = mediator;
            _mapper = mapper;
            _logger = logger;
            
            _eventBus = EventBusProvider.Get();
        }        

        public async Task Handle(UserAuthenticatedEvent notification, CancellationToken cancellationToken)
        {
            var user = notification.User;
            var patient = user.Patient;
            var subscription = patient?.MostRecentSubscription;
            var addOns = patient?.Orders
                .SelectMany(x => x.Items.Select(y => y.AddOn))
                .ToArray();

            var aggregator = patient != null ? await _inputsService.GetAggregatorAsync(patient.GetId()) : null;

            var appointmentsSummary = patient != null ? await _mediator.Send(new GetAppointmentSummaryCommand(patient.GetId()), cancellationToken) : null;

            HealthScoreResponseModel? healthScore = null;

            try
            {
                if (patient != null)
                {
                    healthScore = await _healthScoreService.GetPatientHealthScore(patient.GetId().ToString());
                }
            }
            catch (ClarityCoreException err)
            {
                _logger.LogInformation($"Unable to get health score for patient id: {patient?.Id}, error {err.ToString()}");
            }
             
            var payload = _mapper.Map<UserAuthenticatedPayload>(user);

            foreach (var itemToMap in new List<object?>() 
                     {
                         patient, subscription, addOns != null && addOns.Any() ? addOns : null, 
                         aggregator, appointmentsSummary, healthScore, _environment
                     })
            {
                if (itemToMap != null)
                {
                    _mapper.Map(itemToMap, payload);
                }
            }

            payload.HealthScore = healthScore?.PatientScore?.Score?.ToString() ?? string.Empty;

            await _eventBus.Publish(new UserIntegrationEvent(
                payload: payload,
                user: new UserMetadataModel(user.UniversalId.ToString()),
                eventDate: DateTime.Now), cancellationToken);
        
        }
    }
}
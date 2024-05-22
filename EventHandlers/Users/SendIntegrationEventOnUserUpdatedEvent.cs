using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Events.Users;
using WildHealth.Application.Services.HealthScore;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Users;
using WildHealth.IntegrationEvents.Users.Payloads;
using AutoMapper;
using MediatR;

namespace WildHealth.Application.EventHandlers.Users
{
    public class SendIntegrationEventOnUserUpdatedEvent : INotificationHandler<UserUpdatedEvent>
    {
        private readonly IHealthScoreService _healthScoreService;
        private readonly IWebHostEnvironment _environment;
        private readonly IEventBus _eventBus;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
      
        public SendIntegrationEventOnUserUpdatedEvent(
            IHealthScoreService healthScoreService,
            IWebHostEnvironment environment,
            IMediator mediator, 
            IMapper mapper)
        {
            _healthScoreService = healthScoreService;
            _environment = environment;
            _mediator = mediator;
            _mapper = mapper;
            
            _eventBus = EventBusProvider.Get();
        }     
        public async Task Handle(UserUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var user = notification.User;
            var patient = user.Patient;
            
            var appointmentsSummary = patient != null ? await _mediator.Send(new GetAppointmentSummaryCommand(patient.GetId()), cancellationToken) : null;
            var healthScore =
                patient != null ? await _healthScoreService.GetPatientHealthScore(patient.GetId().ToString()) : null;
            var payload = _mapper.Map<UserUpdatedPayload>(user);
            _mapper.Map(appointmentsSummary, payload);
            _mapper.Map(healthScore, payload);
            _mapper.Map(_environment, payload);
            payload.HealthScore = healthScore?.PatientScore?.Score?.ToString() ?? string.Empty;
            await _eventBus.Publish(new UserIntegrationEvent(
                payload: payload,
                user: new UserMetadataModel(user.UniversalId.ToString()),
                eventDate: DateTime.UtcNow), cancellationToken);
        }
    }
}
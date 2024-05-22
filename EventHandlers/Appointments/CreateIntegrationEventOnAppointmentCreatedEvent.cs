using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using WildHealth.Application.Events.Appointments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Appointments;
using WildHealth.IntegrationEvents.Appointments.Payloads;
using MediatR;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Schedulers.Availability;
using WildHealth.Domain.Models.Appointments;

namespace WildHealth.Application.EventHandlers.Appointments
{
    public class CreateIntegrationEventOnAppointmentCreatedEvent : INotificationHandler<AppointmentCreatedEvent>
    {
        private readonly ISchedulerAvailabilityService _schedulerAvailabilityService;
        private readonly IAppointmentsService _appointmentsService;
        private readonly IEventBus _eventBus;
        private readonly IMapper _mapper;
        
        // This is 35 for a couple of reasons.  
        // 1. Meetings are either 25 or 55 now, so we want to account for those 5 minutes
        // 2. Twilio will round from/to datetimes to nearest hour.  So if we send 2:30, they will round to 3:00.  Want to make sure there's at least an hour between from/to
        private readonly int _minutesBuffer = 35;
        private readonly int _minutesDefaultDuration = 55;

        public CreateIntegrationEventOnAppointmentCreatedEvent(ISchedulerAvailabilityService schedulerAvailabilityService, IAppointmentsService appointmentsService, IMapper mapper, IEventBus eventBus)
        {
            _schedulerAvailabilityService = schedulerAvailabilityService;
            _appointmentsService = appointmentsService;
            _eventBus = eventBus;
            _mapper = mapper;
        }

        public async Task Handle(AppointmentCreatedEvent notification, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentsService.GetByIdAsync(notification.AppointmentId);
            
            var patient = appointment.Patient;
            
            if (patient is null)
            {
                return;
            }

            var appointmentDomain = AppointmentDomain.Create(appointment);

            // This handles scenario where appointment is scheduled very close to now and the DateTime.UtcNow + appointment duration is actually greater than the appointment.StartDate
            // In this situation TimeKit will complain that the from/to window is not big enough for an appointment to fit
            var fromDate = new DateTime(Math.Min(
                DateTime.UtcNow.Ticks,
                appointment.StartDate.AddMinutes(-((appointment.Configuration?.AppointmentType?.Duration ?? _minutesDefaultDuration) + _minutesBuffer)).Ticks
            ));

            // Get other availabilities prior to this one
            var priorAvailabilities = await _schedulerAvailabilityService.GetAvailabilityAsync(
                practiceId: appointmentDomain.GetPracticeId(), 
                configurationId: appointment.ConfigurationId,
                employeeIds: appointment.Employees.Select(o => o.EmployeeId).ToArray(),
                from: fromDate, 
                to: appointment.StartDate
            );

            switch(appointment.WithType)
            {
                case AppointmentWithType.HealthCoach:
                    var payload = _mapper.Map<ScheduledHealthCoachAppointmentPayload>(appointment);
                    _mapper.Map(priorAvailabilities, payload);
                    payload.IsRescheduled = notification.IsRescheduling;
                    payload.Reason = appointment.Reason;
                    payload.Source = notification.Source;
                    
                    await _eventBus.Publish(new AppointmentIntegrationEvent(
                        payload: payload,
                        patient: new PatientMetadataModel(patient.Id.GetValueOrDefault(), patient.User.UserId()),
                        eventDate: appointment.CreatedAt
                        ), cancellationToken);
                    break;
                case AppointmentWithType.Provider:
                case AppointmentWithType.HealthCoachAndProvider:
                    var hcPayload = _mapper.Map<ScheduledMedicalAppointmentPayload>(appointment);
                    _mapper.Map(priorAvailabilities, hcPayload);
                    hcPayload.IsRescheduled = notification.IsRescheduling;
                    hcPayload.Reason = appointment.Reason;
                    hcPayload.Source = notification.Source;
                    
                    await _eventBus.Publish(new AppointmentIntegrationEvent(
                        payload: hcPayload,
                        patient: new PatientMetadataModel(patient.Id.GetValueOrDefault(), patient.User.UserId()),
                        eventDate: appointment.CreatedAt
                        ), cancellationToken);   
                    break;
            }
        }
    }
}


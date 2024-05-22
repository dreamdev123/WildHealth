using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Domain.PatientEngagements.Flows;
using WildHealth.Application.Domain.PatientEngagements.Services;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Extensions.Query;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.EventHandlers.Appointments;

public class CompletePatientEngagementTaskOnAppointmentCreatedEvent : INotificationHandler<AppointmentCreatedEvent>
{
    
    private readonly ILogger<CompletePatientEngagementTaskOnAppointmentCreatedEvent> _logger;
    private readonly IGeneralRepository<Appointment> _appointmentRepository;
    private readonly IPatientEngagementService _engagementService;
    private readonly MaterializeFlow _materializer;

    public CompletePatientEngagementTaskOnAppointmentCreatedEvent(
        IGeneralRepository<Appointment> appointmentRepository,
        IPatientEngagementService engagementService, 
        MaterializeFlow materializer, 
        ILogger<CompletePatientEngagementTaskOnAppointmentCreatedEvent> logger)
    {
        _appointmentRepository = appointmentRepository;
        _engagementService = engagementService;
        _materializer = materializer;
        _logger = logger;
    }

    public async Task Handle(AppointmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Looking for Patient Engagement Task to complete for AppointmentId: {notification.AppointmentId}");
        var appointment = await GetAppointment(notification.AppointmentId);

        if (!appointment.PatientId.HasValue) return;
        
        var patientTasks = await _engagementService.GetHistory(appointment.PatientId.Value.ToArray());
        _logger.LogInformation($"Patient [{appointment.PatientId}] has {patientTasks.Count} tasks in total");
        
        var result = await new AutoCompletePatientEngagementsFlow(appointment.WithType, patientTasks, DateTime.UtcNow).Materialize(_materializer);
        var completedPatientEngagements = result.SelectMany<PatientEngagement>().ToArray();
        _logger.LogInformation($"Completed Patient Engagements count: {completedPatientEngagements.Length}");
    }

    private async Task<LightweightAppointment> GetAppointment(int id) =>
        await _appointmentRepository.All()
            .ById(id)
            .Select(a => new LightweightAppointment(a.PatientId, a.WithType))
            .FindAsync();

    private record LightweightAppointment(int? PatientId, AppointmentWithType WithType);
}
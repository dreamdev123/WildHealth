using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Recommendations;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Metrics;
using WildHealth.Domain.Models.Patient;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.EventHandlers.Subscriptions;

public class UpdatePatientRecommendationsOnSubscriptionCreatedEvent : INotificationHandler<SubscriptionCreatedEvent>
{
    private readonly IMediator _mediator;
    private readonly IGeneralRepository<Patient> _patientRepository;
    private readonly ILogger<UpdatePatientRecommendationsOnSubscriptionCreatedEvent> _logger;

    public UpdatePatientRecommendationsOnSubscriptionCreatedEvent(
        IMediator mediator, 
        IGeneralRepository<Patient> patientRepository,
        ILogger<UpdatePatientRecommendationsOnSubscriptionCreatedEvent> logger)
    {
        _mediator = mediator;
        _patientRepository = patientRepository;
        _logger = logger;
    }

    public async Task Handle(SubscriptionCreatedEvent notification, CancellationToken cancellationToken)
    {
        var sources = Enum.GetValues<MetricSource>();

        var patient = await _patientRepository
            .All()
            .Include(o => o.Subscriptions)
            .Where(o => o.Id == notification.Patient.GetId())
            .FirstOrDefaultAsync(cancellationToken);

        if (patient is null)
        {
            _logger.LogWarning($"Skipping updating patient recommendations since [PatientId] = {notification.Patient.GetId()} did not return a patient");
            
            return;
        }

        var patientDomain = PatientDomain.Create(patient);

        if (patientDomain.SignedUpToday())
        {
            _logger.LogInformation($"Skipping updating patient recommendations since [PatientId] = {notification.Patient.GetId()} just signed up for the first time today and has no meaningful patient data");
            
            return;
        }

        var command = new UpdatePatientRecommendationsCommand(patientId: notification.Patient.GetId(), sources: sources);
        
        await _mediator.Send(command, cancellationToken);
    }
}
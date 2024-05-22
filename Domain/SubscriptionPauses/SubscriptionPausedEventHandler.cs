using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Services.Patients;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using MediatR;

namespace WildHealth.Application.Domain.SubscriptionPauses;

public class SubscriptionPausedEventHandler : INotificationHandler<SubscriptionPausedEvent>
{
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    private readonly IPatientsService _patientsService;

    public SubscriptionPausedEventHandler(
        IIntegrationServiceFactory integrationServiceFactory, 
        IPatientsService patientsService)
    {
        _integrationServiceFactory = integrationServiceFactory;
        _patientsService = patientsService;
    }

    public async Task Handle(SubscriptionPausedEvent @event, CancellationToken cancellationToken)
    {
        var subscription = @event.Subscription;
        var specification = PatientSpecifications.PatientUserSpecification;
        var patient = await _patientsService.GetByIdAsync(subscription.PatientId, specification);
        var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);
        
        await integrationService.PauseSubscriptionAsync(subscription);
    }
}
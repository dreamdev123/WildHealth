using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Subscriptions;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Commands;
using WildHealth.Integration.Factories.IntegrationServiceFactory;

namespace WildHealth.Application.Domain.ManualPayments;

public class CreateManualPaymentCommandHandler : IRequestHandler<CreateManualPaymentCommand>
{
    private readonly IPatientsService _patientsService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly MaterializeFlow _materializer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIntegrationServiceFactory _integrationServiceFactory;

    public CreateManualPaymentCommandHandler(
        IPatientsService patientsService, 
        ISubscriptionService subscriptionService, 
        MaterializeFlow materializer, 
        IDateTimeProvider dateTimeProvider, 
        IIntegrationServiceFactory integrationServiceFactory)
    {
        _patientsService = patientsService;
        _subscriptionService = subscriptionService;
        _materializer = materializer;
        _dateTimeProvider = dateTimeProvider;
        _integrationServiceFactory = integrationServiceFactory;
    }

    public async Task Handle(CreateManualPaymentCommand command, CancellationToken cancellationToken)
    {
        var validatedRequest = ValidatedCreateManualPaymentRequest.Create(command);
        var patient = await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.PatientWithSubscriptionAndIntegrations);
        var subscription = await _subscriptionService.GetAsync(command.SubscriptionId);

        if (!subscription.IsPremium())
            throw new DomainException("Feature not available for none premium patients");
            
        if (subscription.ManualPaymentId != null) // can have only one manual payment for a subscription
            throw new DomainException("Current subscription already has payment schedule");

        var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);
        var invoice = validatedRequest.DownPayment > 0 ? await integrationService.CreatePremiumSubscriptionPaymentAsync(patient, validatedRequest.DownPayment) : null;
        
        await new CreateManualPaymentFlow(subscription, invoice, validatedRequest, _dateTimeProvider.UtcNow()).Materialize(_materializer);
    }
}
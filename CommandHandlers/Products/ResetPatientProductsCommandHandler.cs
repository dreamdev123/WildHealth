using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Services.PatientProducts;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Products.Flows;
using WildHealth.Application.Commands.Products;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Products;

public class ResetPatientProductsCommandHandler : IRequestHandler<ResetPatientProductsCommand>
{
    private readonly IPatientProductsService _patientProductsService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<ResetPatientProductsCommandHandler> _logger;
    private readonly MaterializeFlow _materializeFlow;

    public ResetPatientProductsCommandHandler(
        IPatientProductsService patientProductsService,
        ISubscriptionService subscriptionService,
        ILogger<ResetPatientProductsCommandHandler> logger, 
        MaterializeFlow materializeFlow)
    {
        _patientProductsService = patientProductsService;
        _subscriptionService = subscriptionService;
        _logger = logger;
        _materializeFlow = materializeFlow;
    }

    public async Task Handle(ResetPatientProductsCommand command, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.GetAsync(command.SubscriptionId, SubscriptionSpecifications.CreateBuiltInProductsSpecification);
        var builtInPatientProducts = await _patientProductsService.GetBuiltInProductsForCurrentSubscription(subscription.Patient.GetId());

        var flow = new ResetPatientProductsFlow(subscription, builtInPatientProducts, _logger.LogInformation);
        await flow.Materialize(_materializeFlow);
    }
}
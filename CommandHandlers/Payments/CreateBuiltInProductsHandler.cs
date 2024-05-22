using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Payments;
using WildHealth.Domain.Entities.Patients;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Payments.Flows;
using WildHealth.Application.CommandHandlers.Products.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.Products;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Payments;

public class CreateBuiltInProductsHandler : IRequestHandler<CreateBuiltInProductsCommand, PatientProduct[]>
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPatientProductsService _patientProductsService;
    private readonly IProductsService _productsService;
    private readonly ILogger<CreateBuiltInProductsHandler> _logger;
    private readonly MaterializeFlow _materializeFlow;

    public CreateBuiltInProductsHandler(
        ISubscriptionService subscriptionService,
        ILogger<CreateBuiltInProductsHandler> logger, 
        MaterializeFlow materializeFlow, 
        IPatientProductsService patientProductsService, 
        IProductsService productsService)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
        _materializeFlow = materializeFlow;
        _patientProductsService = patientProductsService;
        _productsService = productsService;
    }

    public async Task<PatientProduct[]> Handle(CreateBuiltInProductsCommand command, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.GetAsync(command.SubscriptionId, SubscriptionSpecifications.CreateBuiltInProductsSpecification);
        var builtInPatientProducts = await _patientProductsService.GetBuiltInProductsForCurrentSubscription(subscription.Patient.GetId());
        var allProducts = await _productsService.GetAsync(subscription.Patient.User.PracticeId);
        var subscriptionProducts = await _patientProductsService.GetSubscriptionProductsAsync(subscription.UniversalId);
        
        var createProductsFlow = new CreateBuiltInProductsFlow(subscription, subscriptionProducts, allProducts);
        
        // Make sure that if some products have already been used that we document that
        var resetProductsFlow = new ResetPatientProductsFlow(subscription, builtInPatientProducts, _logger.LogInformation);
        
        var result = await createProductsFlow.PipeTo(resetProductsFlow).Materialize(_materializeFlow);

        return result.EntityActions
            .Where(action => action is EntityAction.Add && action.Entity is PatientProduct)
            .Select(x => x.Entity<PatientProduct>())
            .ToArray();
    }
}
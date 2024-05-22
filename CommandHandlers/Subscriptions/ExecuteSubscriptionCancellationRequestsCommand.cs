using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using MediatR;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PaymentIssues;

namespace WildHealth.Application.CommandHandlers.Subscriptions
{
    public class ExecuteSubscriptionCancellationRequestsCommandHandler : IRequestHandler<ExecuteSubscriptionCancellationRequestsCommand>
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPracticeService _practiceService;
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        private readonly ILogger<ExecuteSubscriptionCancellationRequestsCommandHandler> _logger;
        private readonly MaterializeFlow _materializeFlow;
        private readonly IPaymentIssuesService _paymentIssuesService;
        
        public ExecuteSubscriptionCancellationRequestsCommandHandler(
            ISubscriptionService subscriptionService,
            IPracticeService practiceService,
            IIntegrationServiceFactory integrationServiceFactory,
            ILogger<ExecuteSubscriptionCancellationRequestsCommandHandler> logger, 
            MaterializeFlow materializeFlow, 
            IPaymentIssuesService paymentIssuesService)
        {
            _subscriptionService = subscriptionService;
            _practiceService = practiceService;
            _integrationServiceFactory = integrationServiceFactory;
            _logger = logger;
            _materializeFlow = materializeFlow;
            _paymentIssuesService = paymentIssuesService;
        }
        
        public async Task Handle(ExecuteSubscriptionCancellationRequestsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Executing of subscription cancellation requests has been started for the date: {request.Date}");
            
            var practices = await _practiceService.GetActiveAsync();
            
            foreach (var practice in practices)
            {
                var subscriptionsToCancel = await _subscriptionService.GetSubscriptionsToCancelAsync(request.Date, practice.GetId());

                _logger.LogInformation($"{subscriptionsToCancel.Count()} subscriptions for practice {practice.Name} are found for cancellation");

                var integrationService = await _integrationServiceFactory.CreateAsync(practice.GetId());
                
                foreach (var subscription in subscriptionsToCancel)
                {
                    await integrationService.TryCancelSubscriptionAsync(
                        subscription: subscription,
                        endDate: subscription.CancellationRequest.Date,
                        reason: subscription.CancellationRequest.Reason
                    );
                    
                    var patientPaymentIssues = await _paymentIssuesService.GetActiveAsync(subscription.PatientId);
                    await new CancelSubscriptionFlow(
                        subscription, 
                        subscription.CancellationRequest.ReasonType, 
                        subscription.CancellationRequest.Reason, 
                        DateTime.UtcNow,
                        patientPaymentIssues).Materialize(_materializeFlow);
                }
            }
            
            _logger.LogInformation($"Executing of subscription cancellation requests has been finished");
        }
    }
}
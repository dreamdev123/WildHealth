using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Services;
using WildHealth.Integration.Services.HintStripe;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Application.Commands.Payments;
using WildHealth.Domain.Enums.Products;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Domain.Models.Patient;

namespace WildHealth.Application.CommandHandlers.Subscriptions;

public class MigrateHintStripeSubscriptionsCommandHandler : IRequestHandler<MigrateHintStripeSubscriptionsCommand>
{
    private readonly ILogger<MigrateHintStripeSubscriptionsCommandHandler> _logger;
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    private readonly IPatientProductsService _patientProductsService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITransactionManager _transactionManager;
    private readonly IPatientsService _patientsService;
    private readonly IMediator _mediator;
    private Action<string>? _outputDelegate;
    private bool _isTestMode;
    
    public MigrateHintStripeSubscriptionsCommandHandler(
        ILogger<MigrateHintStripeSubscriptionsCommandHandler> logger, 
        IIntegrationServiceFactory integrationServiceFactory,
        IPatientProductsService patientProductsService,
        ISubscriptionService subscriptionService,
        ITransactionManager transactionManager,
        IPatientsService patientsService, 
        IMediator mediator)
    {
        _integrationServiceFactory = integrationServiceFactory;
        _patientProductsService = patientProductsService;
        _subscriptionService = subscriptionService;
        _transactionManager = transactionManager;
        _patientsService = patientsService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(MigrateHintStripeSubscriptionsCommand request, CancellationToken cancellationToken)
    {
        _outputDelegate = request.OutputDelegate;
        _isTestMode = request.IsTestMode;
        
        var (patient, currentSubscription) = await GetPatientWithTargetSubscription(request.PatientId);
            
        LogInformation($"Migrating [SubscriptionId]: {currentSubscription.GetId()}, [IntegrationId] = {currentSubscription.Integrations.Select(o => o.Integration).FirstOrDefault()?.Value} from Hint to Stripe with backdating information");

        var integrationService = await GetIntegrationService(patient);

        await _transactionManager.Run(async () =>
        {
            await CancelSubscriptionInHint(integrationService, currentSubscription);
            
            await CreateSubscriptionInStripe(integrationService, currentSubscription);

            await CreatePatientProductsAsync(currentSubscription);
        }, e =>
        {
            LogInformation(
                $"Migrating from Hint to Stripe with backdating information - FAILED - patientId = {patient.GetId()}, subscriptionId = {currentSubscription.GetId()} {e}",
                e);

            var testableOperation = async () =>
            {

                // remove created subscription in stripe.
                await integrationService.DeleteSubscriptionAsync(currentSubscription, IntegrationVendor.Stripe);
            };

            RunTestableOperation(
                new[]
                {
                    $"Attempting to Delete [Subscription] in Stripe"
                }, testableOperation: testableOperation).GetAwaiter().GetResult();
            
        });
    }

    private async Task<(Patient, Subscription)> GetPatientWithTargetSubscription(int patientId)
    {
        var patient = await _patientsService.GetByIdAsync(patientId);
        var patientDomain = PatientDomain.Create(patient);

        if (!patientDomain.IsLinkedWithIntegrationSystem(IntegrationVendor.Hint))
        {
            throw new AppException(HttpStatusCode.BadRequest, "The patient is not linked with Hint.");
        }

        var currentSubscription = patient.CurrentSubscription;
        if (currentSubscription is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "The patient does not hava an active subscription.");
        }
            
        if (currentSubscription.IsLinkedWithIntegrationSystem(IntegrationVendor.Stripe))
        {
            throw new AppException(HttpStatusCode.BadRequest, "Subscription already created in Stripe");
        }

        return (patient, currentSubscription);
    }

    private async Task<IWildHealthIntegrationService> GetIntegrationService(Patient patient)
    {
        var hintStripeIntegrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);

        if (!(hintStripeIntegrationService is WildHealthHintStripeIntegrationService))
        {
            throw new AppException(HttpStatusCode.BadRequest, $"Incorrect integration service was returned");
        }

        return hintStripeIntegrationService;
    }

    private async Task CreatePatientProductsAsync(Subscription subscription)
    {
        var testableOperation = async () =>
        {
            await _mediator.Send(new CreateBuiltInProductsCommand(subscription.GetId()));
        };
        
        await RunTestableOperation(
            new []
            {
                $"CreateBuiltInProductsCommand: [SubscriptionId] = {subscription.GetId()}, [Inclusions] = {String.Join(", ", subscription.GetInclusions().Select(o => o.ProductType.ToString()))}"
            },
            testableOperation);
    }

    private async Task CancelSubscriptionInHint(IWildHealthIntegrationService integrationService, Subscription subscription)
    {
        var testableOperation = async () =>
        {
            var successCancelStatus = await integrationService.TryCancelSubscriptionAsync(subscription, DateTime.UtcNow, "Migrating subscription from Hint to Stripe");

            if (!successCancelStatus)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Unable to cancel subscription in Hint");
            }
        };
        
        await RunTestableOperation(
            new []
            {
                $"TryCancelSubscriptionAsync: [SubscriptionIntegrationId] - {subscription.Integrations.Select(o => o.Integration).FirstOrDefault()?.Value}"
            },
            testableOperation);
    }

    private async Task CreateSubscriptionInStripe(IWildHealthIntegrationService integrationService, Subscription subscription)
    {
        var testableOperation = async () =>
        {
            var result = await integrationService.CreateSubscriptionBackdatedAsync(
                patient: subscription.Patient,
                paymentPrice: subscription.PaymentPrice,
                backdate: subscription.StartDate,
                noStartupFee: true);
                
            subscription.MarksAsPaid(result.Id, IntegrationVendor.Stripe);

            await _subscriptionService.UpdateSubscriptionAsync(subscription); 
        };

        await RunTestableOperation(
            new []
            {
                $"CreateSubscriptionBackdatedAsync: [PatientId] = {subscription.Patient.GetId()}, [PaymentPriceId] = {subscription.PaymentPrice.GetId()}, [backdate] = {subscription.StartDate}, [NoStartupFee] = {true}",
                $"subscription.MarkAsPaid: [subscriptionId] = {subscription.GetId()}, [Vendor] = {IntegrationVendor.Stripe}"
            }, testableOperation);
    }

    private async Task UsePatientProducts(Subscription subscription)
    {
        var patientId = subscription.Patient.GetId();
        
        var appointments = GetSubscriptionVisits(subscription);
        
        var patientProducts = await _patientProductsService.GetBuiltInByPatientAsync(patientId);

        foreach (var appointment in appointments)
        {
            LogInformation($"Checking [AppointmentId] = {appointment.GetId()}");
            
            var availableVisit = patientProducts
                .FirstOrDefault(x => x.ProductType == ProductType.PhysicianVisit && x.CanUseProduct());

            if (availableVisit is null)
            {
                LogInformation($"Unable to find a patient product for [AppointmentId] = {appointment.GetId()}");
                
                return;
            }
            
            LogInformation($"Found [PatientProductId] = {availableVisit.GetId()}");

            availableVisit.UseProduct("appointment migration", appointment.CreatedAt);
            
            LogInformation($"Using [PatientProductId] = {availableVisit.GetId()}, for [Date] = {appointment.CreatedAt}");
        }

        var testableOperation = async () =>
        {
            await _patientProductsService.UpdateAsync(patientProducts.ToArray());
        };

        await RunTestableOperation(
            new []
            {
                $"patientProductsService::UpdateAsync: [PatientProducts::Count] - {patientProducts.Count()}"
            }, testableOperation);
    }

    private Appointment[] GetSubscriptionVisits(Subscription subscription)
    {
        return subscription.Patient.Appointments
            .Where(x => x.StartDate >= subscription.StartDate && x.StartDate < subscription.EndDate)
            .Where(x => x.WithType == AppointmentWithType.HealthCoachAndProvider)
            .Where(x => AppointmentDomain.Create(x).IsCompleted(DateTime.UtcNow))
            .ToArray();
    }
    

    private void LogInformation(string str, Exception? e = null)
    {
        _outputDelegate?.Invoke(str);
        _logger.LogInformation(str, e);
    }
    
    private async Task RunTestableOperation(string[] testableOperationDetails, Func<Task> testableOperation) 
    {
        if (!_isTestMode)
        {
            await testableOperation();
        }

        foreach(var details in testableOperationDetails)
        {
            _outputDelegate?.Invoke($"Running operation: {details}");
        }
    }
}





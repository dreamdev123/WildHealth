using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Insurances;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Integration.Events;
using WildHealth.Integration.Models.Invoices;
using MediatR;
using Microsoft.Extensions.Options;
using Polly;
using WildHealth.Application.Domain.PaymentIssues;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Models.Exceptions;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Factories.IntegrationServiceFactory;

namespace WildHealth.Application.EventHandlers.Integration;

public class IntegrationInvoicePaymentFailedEventHandler : INotificationHandler<IntegrationInvoicePaymentFailedEvent>
{
    private readonly IPatientProductsService _patientProductsService;
    private readonly IClaimsService _claimsService;
    private readonly ILogger _logger;
    private readonly ISubscriptionService _subscriptionService;
    private readonly MaterializeFlow _materializer;
    private readonly IPaymentService _paymentService;
    private readonly IPatientProfileService _patientProfileService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIntegrationsService _integrationsService;
    private readonly PaymentIssueOptions _config;
    private readonly IPatientsService _patientsService;
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    
    public IntegrationInvoicePaymentFailedEventHandler(
        IPatientProductsService patientProductsService,
        IClaimsService claimsService,
        ILogger<IntegrationInvoicePaymentFailedEventHandler> logger,
        ISubscriptionService subscriptionService, 
        MaterializeFlow materializer, 
        IPaymentService paymentService, 
        IPatientProfileService patientProfileService, 
        IDateTimeProvider dateTimeProvider, 
        IIntegrationsService integrationsService,
        IOptions<PaymentIssueOptions> options, 
        IPatientsService patientsService, 
        IIntegrationServiceFactory integrationServiceFactory)
    {
        _patientProductsService = patientProductsService;
        _claimsService = claimsService;
        _logger = logger;
        _subscriptionService = subscriptionService;
        _materializer = materializer;
        _paymentService = paymentService;
        _patientProfileService = patientProfileService;
        _dateTimeProvider = dateTimeProvider;
        _integrationsService = integrationsService;
        _patientsService = patientsService;
        _integrationServiceFactory = integrationServiceFactory;
        _config = options.Value;
    }

    public async Task Handle(IntegrationInvoicePaymentFailedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Processing PaymentFailed event. {@event.Vendor}. InvoiceId: {@event.Invoice.Id}. SubscriptionId: {@event.Invoice.SubscriptionId}");

        var subscription = await WaitForSubscriptionBeingCreated(@event);

        if (subscription == null && !string.IsNullOrEmpty(@event.Invoice.SubscriptionId))
        {
            _logger.LogWarning($"Stripe webhoook for invoice {@event.Invoice.Id} could not find an active subscription with id {@event.Invoice.SubscriptionId}");
            
            // Due to some old bugs we might have inconsistent data so that Clarity subscription is
            // cancelled due to payment issues but Stripe subscription isn't and Stripe still attempts 
            // to charge the patient. To avoid this behaviour we cancel Stripe subscription explicitly here
            // if no active Clarity subscription found.
            await CancelSubscriptionInIntegrationSystem(@event);

            return;
        }
        
        var integration = await _integrationsService.GetAsync(@event.Vendor, GetExternalId(@event)).ToOption();
        if (integration.HasValue())
            await ProcessPaymentIssue(@event, integration.Value());
        else
            _logger.LogInformation($"Integration not found. {@event.Vendor}. InvoiceId: {@event.Invoice.Id}. SubscriptionId: {@event.Invoice.SubscriptionId}");

        await ProcessFailedProductPaymentAsync(@event.Invoice, @event.Vendor);
    }

    #region private
    
    private async Task CancelSubscriptionInIntegrationSystem(IntegrationInvoicePaymentFailedEvent @event)
    {
        var lastActiveSubscription = await _subscriptionService.GetByIntegrationIdAsync(@event.Invoice.SubscriptionId, @event.Vendor, activeOnly: false);
        var integrationService = await _integrationServiceFactory.CreateAsync(lastActiveSubscription.PracticeId!.Value);
        await integrationService.TryCancelSubscriptionAsync(lastActiveSubscription, DateTime.UtcNow, "payment overdue");
    }
    
    private async Task ProcessPaymentIssue(
        IntegrationInvoicePaymentFailedEvent @event, 
        WildHealth.Domain.Entities.Integrations.Integration integration)
    {
        var paymentIssue = integration.PaymentIssues.LastActive(); 
        if (paymentIssue is not null)
        {
            var patientWithIntegration = await _patientsService.GetByIdAsync(paymentIssue.PatientId, PatientSpecifications.PatientWithIntegrations);
            var paymentLink = await _paymentService.CreateResolveCustomerPortalLinkAsync(patientWithIntegration).ToTry(); // Stripe throws for test accounts in dev/stage
            var patientProfileLink = await _patientProfileService.GetProfileLink(paymentIssue.PatientId, paymentIssue.Patient.User.PracticeId);
            
            paymentLink.DoIfError(ex => _logger.LogError("Error during getting payment link. PaymentIssueId: {Id}. Error: {Error}", paymentIssue.GetId(), ex.Message));
            
            var result = await new ProcessPaymentIssueFlow(
                PaymentIssue: paymentIssue,
                NewStatus: PaymentIssueStatus.PatientNotified,
                NotificationTimeWindow: PaymentIssueNotificationTimeWindow.Default,
                Now: _dateTimeProvider.UtcNow(),
                Config: _config,
                PaymentLink: paymentLink.ValueOr(string.Empty),
                PatientProfileLink: patientProfileLink
            ).Materialize(_materializer).ToTry();
            
            result.DoIfError(ex => _logger.LogError("Error during processing subscription payment issue for with Id: {Id}. Error: {Error}", paymentIssue.GetId(), ex.Message));
        }
        else
        {
            var patientId = await _integrationsService.GetPatientIdByClaimUniversalId(integration.ClaimIntegration?.Claim?.ClaimantUniversalId).ToOption(); 
            await new CreatePaymentIssueFlow(integration, _dateTimeProvider.UtcNow(), patientId).Materialize(_materializer);
            var refreshedIntegration = await _integrationsService.GetAsync(@event.Vendor, GetExternalId(@event));
            await ProcessPaymentIssue(@event, refreshedIntegration);
        }
    }
    
    private static string GetExternalId(IntegrationInvoicePaymentFailedEvent @event)
    {
        return @event.Invoice.SubscriptionId ?? @event.Invoice.Id; // for Subscription payment failures we use SubscriptionId and InvoiceId for others
    }
    
    private async Task<Subscription?> WaitForSubscriptionBeingCreated(IntegrationInvoicePaymentFailedEvent notification)
    {
        if (string.IsNullOrEmpty(notification.Invoice.SubscriptionId)) return null;
        
        var policy = Policy
            .Handle<EntityNotFoundException>()
            .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(3) });
        
        // Some payment issue can happen during subscription creation
        // And because of database transactions, subscription can be not in the database yet, when webhook came.
        // For that reason we do retry for EntityNotFoundException to make sure all changes was committed.
        var search = await policy.ExecuteAndCaptureAsync(async () =>
        {
            _logger.LogInformation($"Checking if subscription {notification.Invoice.SubscriptionId} is created");
            var s = await _subscriptionService.GetByIntegrationIdAsync(
                notification.Invoice.SubscriptionId, 
                notification.Vendor, 
                activeOnly: true);
            
            _logger.LogInformation($"Subscription {notification.Invoice.SubscriptionId} is created");
            return s;
        });

        if (search.FinalException != null)
        {
            _logger.LogInformation($"Looking up the subscription {notification.Invoice.SubscriptionId} failed: {search.FinalException}");
        }

        return search.Result;
    }

    private async Task ProcessFailedProductPaymentAsync(InvoiceIntegrationModel invoice, IntegrationVendor vendor)
    {
        var claim = await GetClaimAsync(invoice.Id, vendor);
        if (claim is null)
        {
            _logger.LogInformation($"Claim with [IntegrationId]: {invoice.Id} does not exist");

            return;
        }

        var patientProduct = claim.ClaimantNote?.Appointment?.PatientProduct;

        if (patientProduct is null)
        {
            _logger.LogInformation($"Integration invoice with [IntegrationId]: {invoice.Id} does not contain a patient product");
            
            return;
        }

        patientProduct.MarkAsPendingOutstandingInvoice();
        
        await _patientProductsService.UpdateAsync(patientProduct);
    }
    
    private async Task<Claim?> GetClaimAsync(string integrationId, IntegrationVendor vendor)
    {
        return await _claimsService.GetByIntegrationIdAsync(integrationId, vendor, IntegrationPurposes.Claim.ExternalId);
    }

    #endregion
}
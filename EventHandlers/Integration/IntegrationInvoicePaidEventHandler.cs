using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Domain.PaymentIssues;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Insurances;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.PaymentIssues;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Integration.Events;

namespace WildHealth.Application.EventHandlers.Integration;

public class IntegrationInvoicePaidEventHandler : INotificationHandler<IntegrationInvoicePaidEvent>
{
    private readonly IPatientProductsService _patientProductsService;
    private readonly IClaimsService _claimsService;
    private readonly ILogger<IntegrationInvoicePaidEventHandler> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PaymentIssueOptions _config;
    private readonly MaterializeFlow _materializer;
    private readonly IPaymentIssuesService _paymentIssuesService;
    
    public IntegrationInvoicePaidEventHandler(
        IPatientProductsService patientProductsService,
        IClaimsService claimsService,
        ILogger<IntegrationInvoicePaidEventHandler> logger, 
        IDateTimeProvider dateTimeProvider, 
        IOptions<PaymentIssueOptions> config, 
        MaterializeFlow materializer, 
        IPaymentIssuesService paymentIssuesService)
    {
        _patientProductsService = patientProductsService;
        _claimsService = claimsService;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _config = config.Value;
        _materializer = materializer;
        _paymentIssuesService = paymentIssuesService;
    }

    public async Task Handle(IntegrationInvoicePaidEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Processing InvoicePaid event. {@event.Vendor}. InvoiceId: {@event.Invoice.Id}. SubscriptionId: {@event.Invoice.SubscriptionId}");
        
        await ProcessPaymentIssueIfExists(@event);
        
        await ProcessFailedProductPaymentAsync(@event);
    }

    private async Task ProcessPaymentIssueIfExists(IntegrationInvoicePaidEvent @event)
    {
        var paymentIssue = await _paymentIssuesService.GetByIntegrationExternalIdAsync(GetExternalId(@event)).ToOption();
        if (!paymentIssue.HasValue()) return;
        
        var result = await new ProcessPaymentIssueFlow(
            PaymentIssue: paymentIssue.Value(),
            NewStatus: PaymentIssueStatus.Resolved,
            NotificationTimeWindow: PaymentIssueNotificationTimeWindow.Default,
            Now: _dateTimeProvider.UtcNow(),
            Config: _config
        ).Materialize(_materializer).ToTry();
            
        result.DoIfError(ex => _logger.LogError("Error during processing subscription payment issue for with Id: {Id}. Error: {Error}", paymentIssue.Value().GetId(), ex.Message));
    }

    #region private
    
    private static string GetExternalId(IntegrationInvoicePaidEvent @event)
    {
        return @event.Invoice.SubscriptionId ?? @event.Invoice.Id; // for Subscription payment failures we use SubscriptionId and InvoiceId for others
    }
    
    private async Task ProcessFailedProductPaymentAsync(IntegrationInvoicePaidEvent @event)
    {
        var invoice = @event.Invoice;

        var claim = await GetClaimAsync(invoice.Id, @event.Vendor);
        if (claim is null)
        {
            _logger.LogInformation($"Claim with [IntegrationId]: {invoice.Id} does not exist");

            return;
        }

        var patientProduct = claim.ClaimantNote?.Appointment?.PatientProduct;

        if (patientProduct is null)
        {
            _logger.LogInformation(
                $"Integration invoice with [IntegrationId]: {invoice.Id} does not contain a patient product");

            return;
        }

        patientProduct.MarkAsPaid(invoice.Id);

        await _patientProductsService.UpdateAsync(patientProduct);
    }
    
    private async Task<Claim?> GetClaimAsync(string integrationId, IntegrationVendor vendor)
    {
        return await _claimsService.GetByIntegrationIdAsync(integrationId, vendor, IntegrationPurposes.Claim.ExternalId);
    }

    #endregion
}
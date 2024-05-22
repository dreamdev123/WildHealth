using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Integration.Models.Invoices;

namespace WildHealth.Application.Domain.ManualPayments;

public record UpdatePaymentScheduleItemFlow(PaymentScheduleItem PaymentScheduleItem, InvoiceIntegrationModel Invoice, int PatientId) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        PaymentScheduleItem.InvoiceStatus = Invoice.Status;
        PaymentScheduleItem.PaymentIntegrations = new List<PaymentScheduleItemIntegration>
        {
            new() { Integration = new WildHealth.Domain.Entities.Integrations.Integration(IntegrationVendor.Stripe,"premium subscription", Invoice.Id) }
        };
        
        return PaymentScheduleItem.Updated() + FirePaymentFailedEvent();
    }

    private MaterialisableFlowResult FirePaymentFailedEvent()
    {
        return Invoice.Status.ToLower() != "paid" ? 
            new ScheduledPaymentFailedEvent(PatientId, Invoice.Id, IntegrationVendor.Stripe).ToFlowResult() : 
            MaterialisableFlowResult.Empty;
    }
}
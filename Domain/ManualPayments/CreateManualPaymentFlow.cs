using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Integration.Commands;
using WildHealth.Integration.Models.Invoices;

namespace WildHealth.Application.Domain.ManualPayments;

public record CreateManualPaymentFlow(Subscription Subscription, InvoiceIntegrationModel? DownPaymentInvoice, ValidatedCreateManualPaymentRequest Payload, DateTime Now) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var downPaymentSchedule = DownPaymentInvoice is not null ? new PaymentScheduleItem
        {
            Amount = Payload.DownPayment,
            DueDate = Now,
            InvoiceStatus = DownPaymentInvoice.Status,
            PaymentIntegrations = new List<PaymentScheduleItemIntegration>
            {
                new() { Integration = new WildHealth.Domain.Entities.Integrations.Integration(IntegrationVendor.Stripe, "premium subscription", DownPaymentInvoice.Id) }
            }
        }.ToEnumerable() : Enumerable.Empty<PaymentScheduleItem>();
        
        Subscription.ManualPayment = new Payment
        {
            Name = "premium subscription",
            Total = Payload.Total,
            Deposit = Payload.Deposit,
            DownPayment = Payload.DownPayment,
            RemainingPaidOverMonths = Payload.RemainingPaidOverMonths,
            ScheduleItems = downPaymentSchedule.Concat(Payload.ScheduleItems.Select(x => new PaymentScheduleItem
            {
                Amount = x.Amount,
                DueDate = x.DueDate
            })).ToList()
        };
        
        return Subscription.Updated();
    }
}
using MediatR;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.Domain.ManualPayments;

public record ScheduledPaymentFailedEvent(int PatientId, string IntegrationInvoiceId, IntegrationVendor Vendor) : INotification;

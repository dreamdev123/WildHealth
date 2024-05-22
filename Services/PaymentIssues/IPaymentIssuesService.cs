using System.Threading.Tasks;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Services.PaymentIssues;

public interface IPaymentIssuesService
{
    Task<PaymentIssue[]> GetActiveAsync();
    Task<PaymentIssue[]> GetActiveAsync(int patientId);
    Task<PaymentIssue> GetByIntegrationExternalIdAsync(string integrationId);
    Task<PaymentIssue> GetByOrderIdAsync(int orderId);
    Task<bool> HasOutstandingPayment(int subscriptionId);
}
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.PaymentIssues;

public class PaymentIssuesService : IPaymentIssuesService
{
    private readonly IGeneralRepository<PaymentIssue> _repository;

    public PaymentIssuesService(IGeneralRepository<PaymentIssue> repository)
    {
        _repository = repository;
    }

    public Task<PaymentIssue[]> GetActiveAsync()
    {
        return _repository
            .All()
            .Active()
            .IncludePatient()
            .IncludeSubscription()
            .ToArrayAsync();
    }

    public Task<PaymentIssue[]> GetActiveAsync(int patientId)
    {
        return _repository
            .All()
            .Where(x => x.PatientId == patientId)
            .Active()
            .ToArrayAsync();
    }

    public Task<PaymentIssue> GetByIntegrationExternalIdAsync(string integrationId)
    {
        return _repository
            .All()
            .Active()
            .Where(x => x.Integration.Value == integrationId)
            .Active()
            .IncludePatient()
            .IncludeSubscription()
            .FindAsync();
    }

    public Task<PaymentIssue> GetByOrderIdAsync(int orderId)
    {
        return _repository
            .All()
            .Active()
            .Where(x => x.Integration.OrderInvoiceIntegration.OrderId == orderId)
            .IncludePatient()
            .FindAsync();
    }

    public async Task<bool> HasOutstandingPayment(int subscriptionId)
    {
        return await _repository
            .All()
            .Active()
            .AnyAsync(x => x.Integration.SubscriptionIntegration.Any(s => s.SubscriptionId == subscriptionId));
    }
}
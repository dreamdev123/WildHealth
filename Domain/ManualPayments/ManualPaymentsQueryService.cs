using System.Threading.Tasks;
using WildHealth.Application.Extensions.Query;
using WildHealth.Common.Models.Payments;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Domain.ManualPayments;

public interface IManualPaymentsQueryService
{
    Task<ManualPaymentModel> Get(int manualPaymentId);
}

public class ManualPaymentsQueryService : IManualPaymentsQueryService
{
    private readonly IGeneralRepository<Payment> _paymentRepository;

    public ManualPaymentsQueryService(IGeneralRepository<Payment> paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<ManualPaymentModel> Get(int manualPaymentId)
    {
        var task = await _paymentRepository.All()
            .Query(source => new ManualPaymentQueryFlow(source, manualPaymentId))
            .FindAsync();

        return task;    
    }
}
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.PaymentPrices;

public class PaymentPriceService : IPaymentPriceService
{
    private readonly IGeneralRepository<PaymentPrice> _repository;

    public PaymentPriceService(IGeneralRepository<PaymentPrice> repository)
    {
        _repository = repository;
    }

    public async Task<PaymentPrice> GetAsync(int id)
    {
        var paymentPrice = await _repository
            .All()
            .Include(x => x.PaymentPeriod)
            .ThenInclude(x => x.PaymentPlan)
            .ById(id)
            .FindAsync();

        return paymentPrice;
    }

    public async Task<PaymentPrice[]> GetByPeriodIdAsync(int paymentPeriodId)
    {
        var result = await _repository
            .All()
            .Where(x => 
                x.PaymentPeriodId == paymentPeriodId &&
                (x.Type == PaymentPriceType.Default || x.Type == PaymentPriceType.Insurance))
            .AsNoTracking()
            .ToArrayAsync();

        return result;
    }
}
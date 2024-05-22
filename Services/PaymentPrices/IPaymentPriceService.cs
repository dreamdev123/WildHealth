using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Services.PaymentPrices;

public interface IPaymentPriceService
{
    Task<PaymentPrice> GetAsync(int id);
    
    Task<PaymentPrice[]> GetByPeriodIdAsync(int paymentPeriodId);
}
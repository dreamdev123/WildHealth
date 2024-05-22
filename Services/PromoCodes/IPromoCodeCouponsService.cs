using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Models.Payment;

namespace WildHealth.Application.Services.PromoCodes;

public interface IPromoCodeCouponsService
{ 
    Task<PromoCodeCoupon> CreateAsync(PromoCodeCoupon coupon);
    Task<PromoCodeCoupon?> GetAsync(string? code, int practiceId);
    Task<PromoCodeCoupon?> GetAsync(string code);
    Task<List<PromoCodeCoupon>> GetAsync(int practiceId);
    Task<PromoCodeCoupon?> GetByIdAsync(int id);
    Task DeleteAsync(PromoCodeCoupon coupon);
    Task EditAsync(PromoCodeCoupon? coupon);
    Task<List<PromoCodeDomain>> GetCouponCodesForPlansAsync(int practiceId, PaymentPlan[] paymentPlans);
}
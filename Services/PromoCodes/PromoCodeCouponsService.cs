using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Models.Payment;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Utils.AuthTicket;

namespace WildHealth.Application.Services.PromoCodes;

public class PromoCodeCouponsService : IPromoCodeCouponsService
{
    private readonly IGeneralRepository<PromoCodeCoupon> _repository;
    private readonly IAuthTicket _authTicket;

    public PromoCodeCouponsService(IGeneralRepository<PromoCodeCoupon> repository, IAuthTicket authTicket)
    {
        _repository = repository;
        _authTicket = authTicket;
    }

    private IQueryable<PromoCodeCoupon> NotDeleted() =>
        _repository.All().Where(x => !x.DeletedAt.HasValue);

    public async Task<PromoCodeCoupon> CreateAsync(PromoCodeCoupon coupon)
    {
        await _repository.AddAsync(coupon);
        await _repository.SaveAsync();

        return coupon;
    }

    public async Task<PromoCodeCoupon?> GetAsync(string? code, int practiceId)
    {
        if (string.IsNullOrEmpty(code))
            return null;
        
        var coupon = await NotDeleted()
            .IncludePaymentPlans()
            .FirstOrDefaultAsync(x => x.Code == code && x.PracticeId == practiceId);

        return coupon;
    }

    public async Task<PromoCodeCoupon?> GetAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;
        
        return await GetAsync(code, _authTicket.GetPracticeId());
    }

    public async Task<List<PromoCodeCoupon>> GetAsync(int practiceId)
    {
        return await NotDeleted()
            .Where(x => x.PracticeId == practiceId)
            .IncludePaymentPlans()
            .IncludeSubscriptions()
            .ToListAsync();
    }
    
    public async Task<PromoCodeCoupon?> GetByIdAsync(int id)
    {
        return await _repository
            .All()
            .IncludeSubscriptions()
            .IncludePaymentPlans()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task DeleteAsync(PromoCodeCoupon coupon)
    {
        if (coupon is not null && !coupon.IsDeleted())
        {
            _repository.Delete(coupon);
            await _repository.SaveAsync();
        }
    }

    public async Task EditAsync(PromoCodeCoupon? coupon)
    {
        if (coupon is not null && !coupon.IsDeleted())
        {
            _repository.Edit(coupon);
            await _repository.SaveAsync();
        }
    }

    public async Task<List<PromoCodeDomain>> GetCouponCodesForPlansAsync(int practiceId, PaymentPlan[] paymentPlans)
    {
        var coupons = await GetAsync(practiceId);
        var planIds = paymentPlans.Select(x => x.GetId()).ToArray();
        
        return coupons
            .Select(data => PromoCodeDomain.Create(data, DateTime.UtcNow))
            .Where(domain => planIds.Any(domain.IsCompatible) && domain.CanBeUsed())
            .ToList();
    }
}
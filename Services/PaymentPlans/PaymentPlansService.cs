using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.DistributedCache.Services;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.PromoCodes;

namespace WildHealth.Application.Services.PaymentPlans
{
    /// <summary>
    /// <see cref="IPaymentPlansService"/>
    /// </summary>
    public class PaymentPlansService : IPaymentPlansService
    {
        private readonly IGeneralRepository<PaymentPlan> _paymentPlanRepository;
        private readonly IGeneralRepository<PaymentPeriod> _paymentPeriodsRepository;
        private readonly IGeneralRepository<PaymentPrice> _paymentPricesRepository;
        private readonly IGeneralRepository<PaymentCoupon> _paymentCouponRepository;
        private readonly IGeneralRepository<PaymentPlanInsuranceState> _paymentPlanInsuranceStateRepository;
        private readonly IGeneralRepository<PromoCodeCoupon> _promoCodeCouponsRepository;
        private readonly IWildHealthSpecificCacheService<PaymentPlansService, ICollection<PaymentPlan>> _wildHealthSpecificCachePaymentPlansService;

        public PaymentPlansService(
            IGeneralRepository<PaymentPlan> paymentPlanRepository, 
            IGeneralRepository<PaymentPeriod> paymentPeriodsRepository,
            IGeneralRepository<PaymentPrice> paymentPricesRepository,
            IGeneralRepository<PaymentCoupon> paymentCouponRepository,
            IGeneralRepository<PaymentPlanInsuranceState> paymentPlanInsuranceStateRepository,
            IGeneralRepository<PromoCodeCoupon> promoCodeCouponsRepository,
            IWildHealthSpecificCacheService<PaymentPlansService, ICollection<PaymentPlan>> wildHealthSpecificCachePaymentPlansService)
        {
            _paymentPlanRepository = paymentPlanRepository;
            _paymentPeriodsRepository = paymentPeriodsRepository;
            _paymentPricesRepository = paymentPricesRepository;
            _paymentCouponRepository = paymentCouponRepository;
            _paymentPlanInsuranceStateRepository = paymentPlanInsuranceStateRepository;
            _promoCodeCouponsRepository = promoCodeCouponsRepository;
            _wildHealthSpecificCachePaymentPlansService = wildHealthSpecificCachePaymentPlansService;
        }

        /// <summary>
        /// <see cref="IPaymentPlansService.IsFounderPlanAsync"/>
        /// </summary>
        /// <param name="paymentPeriodId"></param>
        /// <param name="paymentPriceId"></param>
        /// <returns></returns>
        public async Task<bool> IsFounderPlanAsync(int paymentPeriodId, int paymentPriceId)
        {
            var paymentPlan = await _paymentPricesRepository
                .All()
                .Where(x => x.Id == paymentPriceId && x.PaymentPeriodId == paymentPeriodId)
                .Select(x => x.PaymentPeriod.PaymentPlan)
                .FirstOrDefaultAsync();

            if (paymentPlan is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "Payment plan does not exist");
            }

            return paymentPlan.IsFounder;
        }
        
        /// <summary>
        /// <see cref="IPaymentPlansService.GetActiveAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<ICollection<PaymentPlan>> GetActiveAsync(int practiceId, EmployerProduct employerProduct)
        {
            if (employerProduct is not null && !employerProduct.IsDefault)
            {
                var ids = employerProduct.SupportedPaymentPriceIds.ToArray();

                var paymentPrices = await _paymentPricesRepository
                    .All()
                    .ByIds(ids)
                    .Include(o => o.PaymentPeriod)
                    .ThenInclude(o => o.PaymentPlan)
                    .ToArrayAsync();

                var paymentPlanIds = paymentPrices.Select(o => o.PaymentPeriod.PaymentPlan.GetId()).ToArray();

                return await _paymentPlanRepository
                    .All()
                    .ByIds(paymentPlanIds)
                    .AddIncludes()
                    .ToArrayAsync();
            }
            
            var singlePaymentPlans = await _paymentPlanRepository
                    .All()
                    .RelatedToPractice(practiceId)
                    .BySingle()
                    .Active()
                    .OrderBy(x => x.Order)
                    .AddIncludes()
                    .AsNoTracking()
                    .ToArrayAsync();

            if (singlePaymentPlans.Any())
            {
                return singlePaymentPlans;
            }
                
            var paymentPlans = await _wildHealthSpecificCachePaymentPlansService
               .GetAsync($"{practiceId.GetHashCode()}",
                   async () =>
                   {
                       return await _paymentPlanRepository
                           .All()
                           .RelatedToPractice(practiceId)
                           .Active()
                           .OrderBy(x => x.Order)
                           .AddIncludes()
                           .AsNoTracking()
                           .ToArrayAsync();
                   });

            return paymentPlans;
        }
        
        /// <summary>
        /// <see cref="IPaymentPlansService.GetAllAsync(int)"/>
        /// </summary>
        /// <returns></returns>
        public async Task<ICollection<PaymentPlan>> GetAllAsync(int practiceId)
        {
            var paymentPlans = await _paymentPlanRepository
                .All()
                .RelatedToPractice(practiceId)
                .OrderBy(x => x.Order)
                .AddIncludes()
                .AsNoTracking()
                .ToArrayAsync();

            return paymentPlans;
        }

        public async Task<ICollection<PaymentPlan>> GetForPromoCode(int practiceId)
        {
           var singlePaymentPlans = await _paymentPlanRepository
               .All()
               .RelatedToPractice(practiceId)
               .BySingle()
               .AsNoTracking()
               .ToArrayAsync();

           if (singlePaymentPlans.Any())
           {
                    return singlePaymentPlans;
           }
            
           var paymentPlans = await _paymentPlanRepository
                .All()
                .RelatedToPractice(practiceId)
                .AsNoTracking()
                .ToArrayAsync();

            return paymentPlans;
        }

        /// <summary>
        /// <see cref="IPaymentPlansService.GetByIdAsync"/>
        /// </summary>
        /// <param name="paymentPlanId"></param>
        /// <returns></returns>
        public async Task<PaymentPlan> GetByIdAsync(int paymentPlanId)
        {
            var paymentPlan = await _paymentPlanRepository
                .All()
                .ById(paymentPlanId)
                .AddIncludes()
                .FirstOrDefaultAsync();

            if (paymentPlan is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(paymentPlanId), paymentPlanId);
                throw new AppException(HttpStatusCode.NotFound, "Payment plan does not exist", exceptionParam);
            }

            return paymentPlan;
        }

        /// <summary>
        /// <see cref="IPaymentPlansService.GetByIdsAsync(int[], int)"/>
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<ICollection<PaymentPlan>> GetByIdsAsync(int[] ids, int practiceId)
        {
            var paymentPlans = await _paymentPlanRepository
                .All()
                .ByIds(ids)
                .RelatedToPractice(practiceId)
                .OrderBy(x => x.Order)
                .AddIncludes()
                .AsNoTracking()
                .ToArrayAsync();

            return paymentPlans;
        }

        /// <summary>
        /// <see cref="IPaymentPlansService.GetActivePlanAsync"/>
        /// </summary>
        /// <param name="paymentPlanId"></param>
        /// <param name="paymentPeriodId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<PaymentPlan> GetActivePlanAsync(int paymentPlanId, int paymentPeriodId, int practiceId)
        {
            var paymentPlan = await _paymentPlanRepository
                .All()
                .RelatedToPractice(practiceId)
                .Active()
                .AddIncludes()
                .FirstOrDefaultAsync(x => x.Id == paymentPlanId && x.PaymentPeriods.Any(s => s.Id == paymentPeriodId));

            if (paymentPlan is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(paymentPlanId), paymentPlanId);
                throw new AppException(HttpStatusCode.NotFound, "Payment plan does not exist", exceptionParam);
            }

            return paymentPlan;
        }

        /// <summary>
        /// <see cref="IPaymentPlansService.GetPaymentPricesByCouponAsync(string, int, PaymentPriceType)"/>
        /// </summary>
        /// <param name="couponCode"></param>
        /// <param name="paymentPeriodId"></param>
        /// <param name="paymentPriceType"></param>
        /// <returns></returns>
        public async Task<PaymentPrice[]> GetPaymentPricesByCouponAsync(
            string couponCode, 
            int paymentPeriodId, 
            PaymentPriceType paymentPriceType)
        {
            var correspondingPaymentType = paymentPriceType switch
            {
                PaymentPriceType.Default => PaymentPriceType.PromoCode,
                PaymentPriceType.PromoCode => PaymentPriceType.PromoCode,
                PaymentPriceType.Insurance => PaymentPriceType.InsurancePromoCode,
                PaymentPriceType.InsurancePromoCode => PaymentPriceType.InsurancePromoCode,
                _ => throw new ArgumentOutOfRangeException(nameof(paymentPriceType), paymentPriceType, "Unsupported payment price type")
            };
            
            var coupons = await _paymentCouponRepository
                .All()
                .Active()
                .PeriodCoupons(paymentPeriodId)
                .ByPaymentPriceType(correspondingPaymentType)
                .Include(c => c.PaymentPrice)
                .AsNoTracking()
                .ToListAsync();

            coupons = coupons
                .Where(c => c.Code.Equals(couponCode, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
            
            if (!coupons.Any())
            {
                throw new AppException(HttpStatusCode.NotFound, $"Coupon: {couponCode} does not exist");
            }

            var paymentPeriod = await GetActivePeriodAsync(paymentPeriodId);

            var paymentPrices = coupons.Select(x => x.PaymentPrice).ToArray();

            return paymentPeriod
                .Prices
                .Where(x => paymentPrices.Any(k => k.GetId() == x.GetId()))
                .ToArray();
        }

        /// <summary>
        /// <see cref="IPaymentPlansService.GetCouponByPaymentPriceIdAsync"/>
        /// </summary>
        /// <param name="paymentPriceId"></param>
        /// <returns></returns>
        public async Task<PaymentCoupon?> GetCouponByPaymentPriceIdAsync(int paymentPriceId)
        {
            var coupon = await _paymentCouponRepository
                .All()
                .Active()
                .ByPaymentPriceId(paymentPriceId)
                .FirstOrDefaultAsync();

            return coupon;
        }

        /// <summary>
        /// Get the payment price by the recurring payment details
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <param name="amountInDollars"></param>
        /// <param name="interval"></param>
        /// <param name="intervalCount"></param>
        /// <returns></returns>
        public async Task<PaymentPrice> GetPaymentPriceByRecurringDetails(
            string integrationId, 
            IntegrationVendor vendor, 
            string purpose,
            decimal amountInDollars,
            string interval, 
            int intervalCount)
        {
            var paymentStrategy = intervalCount > 1 ? PaymentStrategy.PartialPayment : PaymentStrategy.FullPayment;
                
            var paymentPrice = await _paymentPricesRepository
                .All()
                .ByIntegrationId<PaymentPrice, PaymentPriceIntegration>(integrationId, vendor, purpose)
                .ByPaymentStrategy(paymentStrategy)
                .ByPrice(amountInDollars)
                .IncludePaymentPlan()
                .IncludeIntegrations<PaymentPrice, PaymentPriceIntegration>()
                .FirstOrDefaultAsync();
                
            if (paymentPrice is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(integrationId), integrationId);
                throw new AppException(HttpStatusCode.NotFound, $"Payment price does not exist for [ProductIntegrationId] = {integrationId}", exceptionParam);
            }

            return paymentPrice;
        }

    
        /// <summary>
        /// <see cref="IPaymentPlansService.GetPlanAsync"/>
        /// </summary>
        /// <param name="paymentPlanId"></param>
        /// <param name="paymentPeriodId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<PaymentPlan> GetPlanAsync(int paymentPlanId, int paymentPeriodId, int practiceId)
        {
            var paymentPlan = await _paymentPlanRepository
                .All()
                .RelatedToPractice(practiceId)
                .AddIncludes()
                .FirstOrDefaultAsync(x => 
                    x.Id == paymentPlanId && 
                    x.PaymentPeriods.Any(k => k.Id == paymentPeriodId));

            if (paymentPlan is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(paymentPlanId), paymentPlanId);
                throw new AppException(HttpStatusCode.NotFound, "Payment plan does not exist", exceptionParam);
            }

            return paymentPlan;
        }
        
        /// <summary>
        /// <see cref="IPaymentPlansService.GetPaymentPriceByIntegrationIdAsync"/>
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <param name="price"></param>
        /// <param name="strategy"></param>
        /// <returns></returns>
        public async Task<PaymentPrice> GetPaymentPriceByIntegrationIdAsync(
            string integrationId, 
            IntegrationVendor vendor, 
            string purpose,
            decimal price, 
            PaymentStrategy strategy)
        {
            var paymentPrice = await _paymentPricesRepository
                .All()
                .ByIntegrationId<PaymentPrice, PaymentPriceIntegration>(integrationId, vendor, purpose)
                .ByPaymentStrategy(strategy)
                .ByPrice(price)
                .IncludePaymentPlan()
                .IncludeIntegrations<PaymentPrice, PaymentPriceIntegration>()
                .FirstOrDefaultAsync();

            if (paymentPrice is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(integrationId), integrationId);
                throw new AppException(HttpStatusCode.NotFound, "Payment price does not exist", exceptionParam);
            }

            return paymentPrice;
        }

        /// <summary>
        /// <see cref="IPaymentPlansService.GetPaymentPriceByIdAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<PaymentPrice> GetPaymentPriceByIdAsync(int id)
        {
            var paymentPrice = await _paymentPricesRepository
                .All()
                .IncludePaymentPlan()
                .IncludeIntegrations<PaymentPrice, PaymentPriceIntegration>()
                .FirstOrDefaultAsync(c=> c.Id == id);

            if (paymentPrice is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Payment price does not exist", exceptionParam);
            }

            return paymentPrice;
        }

        /// <summary>
        /// <see cref="IPaymentPlansService.CreatePaymentPlanAsync(PaymentPlan)"/>
        /// </summary>
        /// <param name="paymentPlan"></param>
        /// <returns></returns>
        public async Task<PaymentPlan> CreatePaymentPlanAsync(PaymentPlan paymentPlan)
        {
            await _paymentPlanRepository.AddAsync(paymentPlan);

            await _paymentPlanRepository.SaveAsync();

            return paymentPlan;
        }

        /// <summary>
        /// <see cref="IPaymentPlansService.CreatePaymentPriceAsync"/>
        /// </summary>
        /// <param name="paymentPrice"></param>
        /// <returns></returns>
        public async Task<PaymentPrice> CreatePaymentPriceAsync(PaymentPrice paymentPrice)
        {
            await _paymentPricesRepository.AddAsync(paymentPrice);

            await _paymentPricesRepository.SaveAsync();

            return paymentPrice;
        }
        
        public async Task<PaymentPrice> GetPriceV2(string planName, PaymentStrategy paymentStrategy, bool isInsurance)
        {
            var typesToSearch = isInsurance
                ? new List<PaymentPriceType> { PaymentPriceType.Insurance }
                : new List<PaymentPriceType> { PaymentPriceType.Default };

            var prices = await _paymentPricesRepository
                .All()
                .Where(o => o.PaymentPeriod.PaymentPlan.Name == planName)
                .Where(o => o.Strategy == paymentStrategy)
                .Where(o => typesToSearch.Contains(o.Type))
                .IncludePaymentPlan()
                .ToArrayAsync();

            if (prices.Length > 1)
            {
                prices = prices.Where(o => o.IsActive).ToArray();

                if (prices.Length > 1)
                {
                    throw new AppException(HttpStatusCode.Conflict,
                        $"Multiple prices with this same configuration exist, unable to locate a unique payment price based on this criteria");
                }
            }

            if (prices.Length == 0)
            {
                throw new AppException(HttpStatusCode.NotFound,
                    $"Unable to find a payment price with this given criteria");
            }

            return prices.First();
        }

        /// <summary>
        /// Returns all PaymentCoupons for payment plans
        /// </summary>
        /// <param name="paymentPlans"></param>
        /// <returns></returns>
        public async Task<PaymentCoupon[]> GetPaymentCouponCodesForPlans(PaymentPlan[] paymentPlans)
        {
            var paymentPlanIds = paymentPlans.Select(o => o.GetId());

            return await _paymentCouponRepository
                .All()
                .Where(o => paymentPlanIds.Contains(o.PaymentPrice.PaymentPeriod.PaymentPlanId))
                .ToArrayAsync();
        }

        public async Task<PaymentPlan?> GetByPaymentPeriodId(int paymentPeriodId)
        {
            var paymentPlan = await _paymentPlanRepository
                .All()
                .Include(x => x.PaymentPeriods)
                .ThenInclude(x => x.Prices)
                .FirstOrDefaultAsync(x => x.PaymentPeriods.Any(pp => pp.Id == paymentPeriodId));

            return paymentPlan;
        }

        public async Task<PaymentPlanInsuranceState> CreatePaymentPlanInsuranceStateAsync(PaymentPlanInsuranceState paymentPlanInsuranceState)
        {
            await _paymentPlanInsuranceStateRepository.AddAsync(paymentPlanInsuranceState);
            
            await _paymentPlanInsuranceStateRepository.SaveAsync();

            return paymentPlanInsuranceState;
        }
        
        public async Task<PaymentPlanInsuranceState> DeletePaymentPlanInsuranceStateAsync(int id)
        {
            var paymentPlanInsuranceState = await _paymentPlanInsuranceStateRepository.GetAsync(id);
            
            _paymentPlanInsuranceStateRepository.Delete(paymentPlanInsuranceState);

            await _paymentPlanInsuranceStateRepository.SaveAsync();

            return paymentPlanInsuranceState;
        }

        #region private

        private async Task<PaymentPeriod> GetActivePeriodAsync(int id)
        {
            var paymentPeriod = await _paymentPeriodsRepository
                .All()
                .ById(id)
                .Active()
                .AddIncludes()
                .FirstOrDefaultAsync();

            if (paymentPeriod is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Payment period does not exist", exceptionParam);
            }

            return paymentPeriod;
        }

        #endregion
    }
}

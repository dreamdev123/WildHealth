using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Payments;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Utils.PatientProductsFactory;
using WildHealth.Application.Services.Subscriptions;
using MediatR;
using WildHealth.Domain.Models.Payment;

namespace WildHealth.Application.CommandHandlers.Payments
{
    public class ApplyCouponProductsCommandHandler : IRequestHandler<ApplyCouponProductsCommand, bool>
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPatientsProductFactory _patientsProductFactory;
        private readonly IPatientProductsService _patientProductsService;
        private readonly ILogger _logger;

        public ApplyCouponProductsCommandHandler(
            ISubscriptionService subscriptionService,
            IPatientsProductFactory patientsProductFactory,
            IPatientProductsService patientProductsService,
            ILogger<ApplyCouponProductsCommandHandler> logger)
        {
            _subscriptionService = subscriptionService;
            _patientsProductFactory = patientsProductFactory;
            _patientProductsService = patientProductsService;
            _logger = logger;
        }

        public async Task<bool> Handle(ApplyCouponProductsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Processing of applying coupon products for patient with [Id] = {request.Patient.Id} started.");

            var patientProducts = await GetProductProductsAsync(request.Patient, request.PaymentPriceId);

            if (patientProducts is null || !patientProducts.Any())
            {
                return true;
            }

            await _patientProductsService.CreateAsync(patientProducts);

            return true;
        }
        
        #region private

        private async Task<PatientProduct[]> GetProductProductsAsync(Patient patient, int? paymentPriceId)
        {
            if (paymentPriceId is null)
            {
                return Array.Empty<PatientProduct>();
            }

            return await GetProductsAsyncV2(patient, paymentPriceId);
        }

        private async Task<PatientProduct[]> GetProductsAsyncV2(Patient patient, int? paymentPriceId)
        {
            var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(patient.GetId());
            
            var coupon = PromoCodeDomain.Create(subscription.PromoCodeCoupon, DateTime.UtcNow);

            var freeAddOns = coupon.GetFreeAddOns();

            if (freeAddOns.Any())
            {
                return await _patientsProductFactory.CreateBuildInAsyncV2(
                    patient: patient, 
                    subscription: subscription,
                    coupon: coupon
                );
            }

            return Array.Empty<PatientProduct>();
        }

        #endregion
    }
}

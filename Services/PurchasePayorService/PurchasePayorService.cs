using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Interfaces;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.PurchasePayorService
{
    /// <summary>
    /// Provides methods for application purchasePayor
    /// </summary>
    public class PurchasePayorService : IPurchasePayorService
    {
        private readonly IGeneralRepository<PurchasePayor> _purchasePayorRepository;
        
        public PurchasePayorService(
            IGeneralRepository<PurchasePayor> purchasePayorRepository)
        {
            _purchasePayorRepository = purchasePayorRepository;
        }

        /// <summary>
        /// <see cref="IPurchasePayorService.CreateAsync"/>
        /// </summary>
        /// <param name="payable"></param>
        /// <param name="payor"></param>
        /// <param name="patient"></param>
        /// <param name="amount"></param>
        /// <param name="billableOnDate"></param>
        /// <param name="isBilled"></param>
        /// <returns></returns>
        public async Task<PurchasePayor> CreateAsync(IPayable payable,
            IPayor? payor,
            Patient patient,
            decimal amount,
            DateTime? billableOnDate,
            bool isBilled = false)
        {
            if (payable == null)
            {
                throw new AppException(HttpStatusCode.NotFound,
                    $"Cannot record a partial payment because the product/service was not provided");
            }

            if (payor == null)
            {
                throw new AppException(HttpStatusCode.NotFound,
                    $"Cannot record a partial payment because the payor was not provided");
            }

            if (patient == null)
            {
                throw new AppException(HttpStatusCode.NotFound,
                    $"Cannot record a partial payment because the patient was not provided");
            }
            
            var purchasePayor = new PurchasePayor()
            {
                PayableUniversalId = payable.UniversalId,
                PayorUniversalId = payor.UniversalId,
                PatientId = patient.GetId(),
                Amount = amount,
                BillableOnDate = billableOnDate,
                IsBilled = isBilled
            };

            await _purchasePayorRepository.AddAsync(purchasePayor);
            await _purchasePayorRepository.SaveAsync();

            return purchasePayor;
        }

        /// <summary>
        /// <see cref="IPurchasePayorService.SelectAsync"/>
        /// </summary>
        /// <param name="payableUniversalId"></param>
        /// <returns></returns>
        public Task<PurchasePayor[]> SelectAsync(Guid payableUniversalId)
        {
            return _purchasePayorRepository
                .All()
                .Where(x => x.PayableUniversalId == payableUniversalId)
                .ToArrayAsync();
        }
    }
}
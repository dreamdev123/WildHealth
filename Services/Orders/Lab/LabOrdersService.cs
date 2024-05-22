using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Orders.Lab
{
    /// <summary>
    /// <see cref="ILabOrdersService"/>
    /// </summary>
    public class LabOrdersService : ILabOrdersService
    {
        private readonly IGeneralRepository<LabOrder> _labOrdersRepository;

        public LabOrdersService(IGeneralRepository<LabOrder> labOrdersRepository)
        {
            _labOrdersRepository = labOrdersRepository;
        }

        /// <summary>
        /// <see cref="ILabOrdersService"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<LabOrder[]> GetPatientOrdersAsync(int patientId)
        {
            var orders = await _labOrdersRepository
                .All()
                .RelatedToPatient(patientId)
                .OrderBy(x => x.OrderedAt)
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .IncludePatientSubscriptions()
                .IncludeIntegrationInvoice()
                .IncludeCancelledBy()
                .AsNoTracking()
                .ToArrayAsync();

            return orders;
        }
        
        /// <summary>
        /// <see cref="ILabOrdersService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<LabOrder> GetByIdAsync(int id)
        {
            var order = await _labOrdersRepository
                .All()
                .ById(id)
                .IncludeOrderItemsWithAddOns()
                .IncludePatientAndIntegrations()
                .IncludePatientSubscriptions()
                .IncludePatientLocation()
                .IncludeIntegrationInvoice()
                .IncludeCancelledBy()
                .FirstOrDefaultAsync();

            if (order is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Order does not exist.", exceptionParam);
            }
            
            return order;
        }

        /// <summary>
        /// <see cref="ILabOrdersService.GetByNumberAsync"/>
        /// </summary>
        /// <param name="number"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<LabOrder> GetByNumberAsync(string number, int patientId)
        {
            var order = await _labOrdersRepository
                .All()
                .ByNumber(number)
                .RelatedToPatient(patientId)
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .FirstOrDefaultAsync();

            if (order is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Order with number: ${number} does not exist.");
            }
            
            return order;
        }

        /// <summary>
        /// <see cref="ILabOrdersService.GetByExpectedCollectionDate"/>
        /// </summary>
        /// <param name="expectedCollectionDate"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<LabOrder[]> GetByExpectedCollectionDate(DateTime expectedCollectionDate)
        {
            var orders = await _labOrdersRepository
                .All()
                .ByStatus(OrderStatus.Placed)
                .ByCollectionDate(expectedCollectionDate)
                .IncludePatient()
                .ToArrayAsync();

            return orders;
        }

        /// <summary>
        /// <see cref="ILabOrdersService.UpdateAsync"/>
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<LabOrder> UpdateAsync(LabOrder order)
        {
            _labOrdersRepository.Edit(order);

            await _labOrdersRepository.SaveAsync();

            return order;
        }

        public async Task<int?> GetCurrentOrderIdAsync(int? patientId)
        {
            var expectedStatuses = new []{ OrderStatus.Placed, OrderStatus.Completed };
            return (await _labOrdersRepository.All()
                .RelatedToPatient(patientId)
                .OrderBy(x => x.ExpectedCollectionDate)
                .FirstOrDefaultAsync(x => expectedStatuses.Contains(x.Status)))?.Id;
        }
    }
}
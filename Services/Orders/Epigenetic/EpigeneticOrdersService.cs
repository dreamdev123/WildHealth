using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.AddOns;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;

namespace WildHealth.Application.Services.Orders.Epigenetic
{
    /// <summary>
    /// <see cref="IEpigeneticOrdersService"/>
    /// </summary>
    public class EpigeneticOrdersService : IEpigeneticOrdersService
    {
        private readonly IGeneralRepository<EpigeneticOrder> _epigeneticOrdersRepository;

        public EpigeneticOrdersService(IGeneralRepository<EpigeneticOrder> epigeneticOrdersRepository)
        {
            _epigeneticOrdersRepository = epigeneticOrdersRepository;
        }

        /// <summary>
        /// <see cref="IEpigeneticOrdersService.GetAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<EpigeneticOrder> GetAsync(int id)
        {
            return _epigeneticOrdersRepository
                .All()
                .ById(id)
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .FindAsync();
        }

        /// <summary>
        /// <see cref="IEpigeneticOrdersService.GetPatientOrdersAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<EpigeneticOrder[]> GetPatientOrdersAsync(int patientId)
        {
            var orders = await _epigeneticOrdersRepository
                .All()
                .RelatedToPatient(patientId)
                .OrderBy(x => x.OrderedAt)
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .AsNoTracking()
                .ToArrayAsync();

            return orders;
        }

        /// <summary>
        /// <see cref="IEpigeneticOrdersService.SelectOrdersAsync"/>
        /// </summary>
        /// <param name="statuses"></param>
        /// <param name="providers"></param>
        /// <returns></returns>
        public Task<EpigeneticOrder[]> SelectOrdersAsync(OrderStatus[] statuses, AddOnProvider[] providers)
        {
            return _epigeneticOrdersRepository
                .All()
                .ByStatuses(statuses)
                .ByProviders(providers)
                .OrderBy(x => x.OrderedAt)
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IEpigeneticOrdersService.CreateAsync"/>
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<EpigeneticOrder> CreateAsync(EpigeneticOrder order)
        {
            await _epigeneticOrdersRepository.AddAsync(order);

            await _epigeneticOrdersRepository.SaveAsync();

            return order;
        }

        /// <summary>
        /// <see cref="IEpigeneticOrdersService.UpdateAsync"/>
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<EpigeneticOrder> UpdateAsync(EpigeneticOrder order)
        {
            _epigeneticOrdersRepository.Edit(order);

            await _epigeneticOrdersRepository.SaveAsync();

            return order;
        }
    }
}
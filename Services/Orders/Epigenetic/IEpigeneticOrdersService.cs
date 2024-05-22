using System.Threading.Tasks;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.AddOns;
using WildHealth.Domain.Enums.Orders;

namespace WildHealth.Application.Services.Orders.Epigenetic
{
    /// <summary>
    /// Provides methods for working with epigenetic orders
    /// </summary>
    public interface IEpigeneticOrdersService
    {
        /// <summary>
        /// Returns epigenetic order by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<EpigeneticOrder> GetAsync(int id);
        
        /// <summary>
        /// Returns patient epigenetic orders
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<EpigeneticOrder[]> GetPatientOrdersAsync(int patientId);
        
        /// <summary>
        /// Returns patient epigenetic orders
        /// </summary>
        /// <param name="statuses"></param>
        /// <param name="providers"></param>
        /// <returns></returns>
        Task<EpigeneticOrder[]> SelectOrdersAsync(OrderStatus[] statuses, AddOnProvider[] providers);
        
        /// <summary>
        /// Creates epigenetic order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<EpigeneticOrder> CreateAsync(EpigeneticOrder order);
        
        /// <summary>
        /// Updates epigenetic order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<EpigeneticOrder> UpdateAsync(EpigeneticOrder order);
    }
}
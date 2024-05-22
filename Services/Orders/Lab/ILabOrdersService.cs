using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Orders;

namespace WildHealth.Application.Services.Orders.Lab
{
    /// <summary>
    /// Provides methods for working with lab orders
    /// </summary>
    public interface ILabOrdersService
    {
        /// <summary>
        /// Returns patient Lab orders
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<LabOrder[]> GetPatientOrdersAsync(int patientId);
        
        /// <summary>
        /// Returns Lab order by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<LabOrder> GetByIdAsync(int id);

        /// <summary>
        /// Returns order by order number
        /// </summary>
        /// <param name="number"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<LabOrder> GetByNumberAsync(string number, int patientId);
        
        /// <summary>
        /// Returns order by expected collection date
        /// </summary>
        /// <param name="expectedCollectionDate"></param>
        /// <returns></returns>
        Task<LabOrder[]> GetByExpectedCollectionDate(DateTime expectedCollectionDate);
        
        /// <summary>
        /// Updates Lab order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<LabOrder> UpdateAsync(LabOrder order);
        
        Task<int?> GetCurrentOrderIdAsync(int? patientId);
    }
}
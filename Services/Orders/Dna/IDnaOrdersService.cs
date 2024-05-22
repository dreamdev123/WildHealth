using System;
using System.Threading.Tasks;
using WildHealth.Common.Models.Integration.ImageMark;
using WildHealth.Common.Models.Orders;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;

namespace WildHealth.Application.Services.Orders.Dna
{
    /// <summary>
    /// Provides methods for working with dna orders
    /// </summary>
    public interface IDnaOrdersService
    {
        /// <summary>
        /// Returns if dna order was replaced
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> IsReplacedAsync(int id);
        
        /// <summary>
        /// Returns dna order by barcode
        /// </summary>
        /// <param name="barcode"></param>
        /// <returns></returns>
        Task<DnaOrder> GetByBarcodeAsync(string barcode);
        
        /// <summary>
        /// Returns filtered orders without practice id
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="statuses"></param>
        /// <returns></returns>
        Task<DnaOrder[]> SelectForIntegrationAsync(
            DateTime from, 
            DateTime to,
            OrderStatus[] statuses);

        /// <summary>
        /// Returns DNA orders ready for publishing
        /// </summary>
        /// <returns></returns>
        Task<ReadyForPublishDnaOrderModel[]> GetReadyForPublishAsync();
        
        /// <summary>
        /// Returns patient DNA orders
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<DnaOrder[]> GetAsync(int patientId);

        /// <summary>
        /// Returns DNA order by number
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        Task<DnaOrder> GetByNumberAsync(string number);
        
        /// <summary>
        /// Returns DNA order by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<DnaOrder> GetByIdAsync(int id);

        /// <summary>
        /// Returns dna order in progress
        /// </summary>
        /// <returns></returns>
        Task<DnaOrder[]> GetActiveAsync();
        
        /// <summary>
        /// Creates dna order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<DnaOrder> CreateAsync(DnaOrder order);
        
        /// <summary>
        /// Updates dna order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<DnaOrder> UpdateAsync(DnaOrder order);
        
        /// <summary>
        /// Creates Manual dna order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<DnaOrder> CreateAsync(ManualDnaOrder order);
        
        /// <summary>
        /// Updates a Manual dna order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<DnaOrder> UpdateAsync(ManualDnaOrder order);
        
        /// <summary>
        /// Returns DNA orders by OrderStatus
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        Task<DnaOrder[]> GetByOrderStatus(int practiceId, OrderStatus status);

        /// <summary>
        /// Returns DNA orders by OrderStatus for purpose of DNA Dropshipping
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        Task<DnaOrderModel[]> GetByOrderStatusForDropship(int practiceId, OrderStatus status);
    }
}
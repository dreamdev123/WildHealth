using System.Threading.Tasks;
using WildHealth.Domain.Entities.Orders;

namespace WildHealth.Application.Services.Orders.Other;

/// <summary>
/// Provides methods for working with simple orders
/// </summary>
public interface IOtherOrdersService
{
    /// <summary>
    /// Returns order by identifier
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<OtherOrder> GetAsync(int id);
    
    /// <summary>
    /// Returns patient orders
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    Task<OtherOrder[]> GetPatientOrdersAsync(int patientId);
    
    /// <summary>
    /// Returns orders for review
    /// </summary>
    /// <param name="employeeId"></param>
    /// <returns></returns>
    Task<OtherOrder[]> GetOrdersForReviewAsync(int employeeId);
}
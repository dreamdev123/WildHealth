using System.Threading.Tasks;
using WildHealth.Domain.Entities.Orders;

namespace WildHealth.Application.Services.Orders.Referral;

/// <summary>
/// Provides methods for working with referral orders
/// </summary>
public interface IReferralOrdersService
{
    /// <summary>
    /// Returns order by identifier
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ReferralOrder> GetAsync(int id);
    
    /// <summary>
    /// Returns patient Lab orders
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    Task<ReferralOrder[]> GetPatientOrdersAsync(int patientId);
    
    /// <summary>
    /// Returns orders for review
    /// </summary>
    /// <param name="employeeId"></param>
    /// <returns></returns>
    Task<ReferralOrder[]> GetOrdersForReviewAsync(int employeeId);
}
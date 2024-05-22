using System.Threading.Tasks;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Services;

public interface IPreauthorizeRequestsService
{
    Task<PreauthorizeRequest> GetByEmailAsync(string email);
    
    Task<PreauthorizeRequest> GetByTokenAsync(string token);
    
    Task<PreauthorizeRequest> GetByIdAsync(int id);
    
    Task<PreauthorizeRequest[]> GetByIdsAsync(int[] ids);
    
    Task<PreauthorizeRequest[]> GetAsync(
        int practiceId,
        int? paymentPlanId = null,
        int? paymentPeriodId = null,
        int? paymentPriceId = null,
        int? employerProductId = null
    );
}
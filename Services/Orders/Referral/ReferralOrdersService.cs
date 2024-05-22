using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Orders.Referral;

/// <summary>
/// <see cref="IReferralOrdersService"/>
/// </summary>
public class ReferralOrdersService : IReferralOrdersService
{
    private readonly IGeneralRepository<ReferralOrder> _referralOrdersRepository;

    public ReferralOrdersService(IGeneralRepository<ReferralOrder> referralOrdersRepository)
    {
        _referralOrdersRepository = referralOrdersRepository;
    }

    /// <summary>
    /// <see cref="IReferralOrdersService.GetAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<ReferralOrder> GetAsync(int id)
    {
        return _referralOrdersRepository
            .All()
            .ById(id)
            .IncludeOrderItemsWithAddOns()
            .IncludeOrderData()
            .IncludeReviewer()
            .IncludePatient()
            .FindAsync();
    }

    /// <summary>
    /// <see cref="IReferralOrdersService.GetPatientOrdersAsync"/>
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    public async Task<ReferralOrder[]> GetPatientOrdersAsync(int patientId)
    {
        var orders = await _referralOrdersRepository
            .All()
            .RelatedToPatient(patientId)
            .OrderBy(x => x.OrderedAt)
            .IncludeOrderItemsWithAddOns()
            .IncludeOrderData()
            .IncludeReviewer()
            .IncludePatient()
            .AsNoTracking()
            .ToArrayAsync();

        return orders;
    }

    /// <summary>
    /// <see cref="IReferralOrdersService.GetOrdersForReviewAsync"/>
    /// </summary>
    /// <param name="employeeId"></param>
    /// <returns></returns>
    public async Task<ReferralOrder[]> GetOrdersForReviewAsync(int employeeId)
    {
        var orders = await _referralOrdersRepository
            .All()
            .UnderReviewBy(employeeId)
            .OrderBy(x => x.OrderedAt)
            .IncludeOrderItemsWithAddOns()
            .IncludeOrderData()
            .IncludeReviewer()
            .IncludePatient()
            .AsNoTracking()
            .ToArrayAsync();

        return orders;
    }
}
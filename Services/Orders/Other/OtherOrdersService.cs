using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Orders.Other;

/// <summary>
/// <see cref="IOtherOrdersService"/>
/// </summary>
public class OtherOrdersService : IOtherOrdersService
{
    private readonly IGeneralRepository<OtherOrder> _otherOrdersRepository;

    public OtherOrdersService(IGeneralRepository<OtherOrder> otherOrdersRepository)
    {
        _otherOrdersRepository = otherOrdersRepository;
    }

    /// <summary>
    /// <see cref="IOtherOrdersService.GetAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<OtherOrder> GetAsync(int id)
    {
        return _otherOrdersRepository
            .All()
            .ById(id)
            .IncludeOrderItemsWithAddOns()
            .IncludeOrderData()
            .IncludeReviewer()
            .IncludePatient()
            .FindAsync();
    }

    /// <summary>
    /// <see cref="IOtherOrdersService.GetPatientOrdersAsync"/>
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    public async Task<OtherOrder[]> GetPatientOrdersAsync(int patientId)
    {
        var orders = await _otherOrdersRepository
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
    /// <see cref="IOtherOrdersService.GetOrdersForReviewAsync"/>
    /// </summary>
    /// <param name="employeeId"></param>
    /// <returns></returns>
    public async Task<OtherOrder[]> GetOrdersForReviewAsync(int employeeId)
    {
        var orders = await _otherOrdersRepository
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
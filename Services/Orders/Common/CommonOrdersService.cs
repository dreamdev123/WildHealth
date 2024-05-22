using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Notes.Common;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;
using WildHealth.Application.Extensions.Query;
using WildHealth.Common.Models.Orders;

namespace WildHealth.Application.Services.Orders.Common;

/// <summary>
/// <see cref="ICommonOrdersService"/>
/// </summary>
public class CommonOrdersService : ICommonOrdersService
{
    private readonly IGeneralRepository<CommonOrder> _commonOrdersRepository;

    public CommonOrdersService(IGeneralRepository<CommonOrder> commonOrdersRepository)
    {
        _commonOrdersRepository = commonOrdersRepository;
    }

    /// <summary>
    /// <see cref="ICommonOrdersService.GetByIdAsync(int)"/>
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    public async Task<CommonOrder> GetByIdAsync(int orderId)
    {
        return await _commonOrdersRepository
            .All()
            .ById(orderId)
            .FindAsync();
    }

    /// <summary>
    /// <see cref="ICommonOrdersService.GetCommonOrdersAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonOrder[]> GetCommonOrdersAsync()
    {
        var commonOrders =  await _commonOrdersRepository
            .All()
            .ToArrayAsync();

        return commonOrders;
    }

    /// <summary>
    /// <see cref="ICommonOrdersService.CreateCommonOrderAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonOrder> CreateCommonOrderAsync(CommonOrderModel model)
    {
        var newCommonOrder = new CommonOrder
        {
            Company = model.Company,
            Name = model.Name,
            Cost = model.Cost
        };

        await _commonOrdersRepository.AddAsync(newCommonOrder);

        await _commonOrdersRepository.SaveAsync();

        return newCommonOrder;
    }

    /// <summary>
    /// <see cref="ICommonOrdersService.UpdateCommonOrderAsync"/>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<CommonOrder> UpdateCommonOrderAsync(CommonOrderModel model)
    {
        var commonOrder = await GetByIdAsync(model.Id);

        commonOrder.Company = model.Company;
        commonOrder.Name = model.Name;
        commonOrder.Cost = model.Cost;

        _commonOrdersRepository.Edit(commonOrder);

        await _commonOrdersRepository.SaveAsync();

        return commonOrder;
    }

    /// <summary>
    /// <see cref="ICommonOrdersService.DeleteCommonOrderAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CommonOrder> DeleteCommonOrderAsync(int id)
    {
        var commonOrder = await GetByIdAsync(id);

        _commonOrdersRepository.Delete(commonOrder);

        await _commonOrdersRepository.SaveAsync();

        return commonOrder;
    }
}
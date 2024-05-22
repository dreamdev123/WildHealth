using System.Threading.Tasks;
using WildHealth.Domain.Entities.Notes.Common;
using WildHealth.Common.Models.Orders;

namespace WildHealth.Application.Services.Orders.Common;

/// <summary>
/// Provides methods for working with simple orders
/// </summary>
public interface ICommonOrdersService
{
    /// <summary>
    /// Returns Common order by Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<CommonOrder> GetByIdAsync(int id);

    /// <summary>
    /// Returns all Common orders
    /// </summary>
    /// <returns></returns>
    Task<CommonOrder[]> GetCommonOrdersAsync();

    /// <summary>
    /// creates common order
    /// <param name="model"></param>
    /// </summary>
    /// <returns></returns>
    Task<CommonOrder> CreateCommonOrderAsync(CommonOrderModel model);

    /// <summary>
    /// Returns an updated Common order
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<CommonOrder> UpdateCommonOrderAsync(CommonOrderModel model);

    /// <summary>
    /// Deletes Common order
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<CommonOrder> DeleteCommonOrderAsync(int id);
}
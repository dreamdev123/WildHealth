using System.Threading.Tasks;
using WildHealth.Common.Models.ShippingOrders;
using WildHealth.ShipStation.Clients.Models;

namespace WildHealth.Application.Services.ShippingOrders;

public interface IShippingOrderService
{
    /// <summary>
    /// Create orders
    /// </summary>
    /// <param name="orders"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<CreateOrdersResponseModel> CreateOrders(CreateOrderModel[] orders, int practiceId);

    /// <summary>
    /// Get shipment
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<ShipmentModel> GetShipment(int orderId, int practiceId);

    /// <summary>
    /// Get tracking information
    /// </summary>
    /// <param name="carrierCode"></param>
    /// <param name="trackingNumber"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<TrackingModel> GetTracking(string carrierCode, string trackingNumber, int practiceId);
}
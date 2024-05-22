using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.ShippingOrders;
using WildHealth.Common.Options;
using WildHealth.Settings;
using WildHealth.Shared.DistributedCache.Services;
using WildHealth.ShipEngine.Clients.Models;
using WildHealth.ShipEngine.Clients.WebClients;
using WildHealth.ShipStation.Clients.Credentials;
using WildHealth.ShipStation.Clients.Models;
using WildHealth.ShipStation.Clients.WebClients;

namespace WildHealth.Application.Services.ShippingOrders;

public class ShippingOrderService : IShippingOrderService
{
    private readonly IShipStationOrdersWebClient _ordersWebClient;
    private readonly IShipEngineTrackingWebClient _trackingWebClient;
    private readonly IWildHealthSpecificCacheService<ShippingOrderService, TrackingResponseModel> _trackingResponseCacheService; 
    private readonly IMapper _mapper;
    private readonly ISettingsManager _settingsManager;
    private readonly ILogger<ShippingOrderService> _logger;

    public ShippingOrderService(
        IShipStationOrdersWebClient ordersWebClient,
        IShipEngineTrackingWebClient trackingWebClient,
        IWildHealthSpecificCacheService<ShippingOrderService, TrackingResponseModel> trackingResponseCacheService,
        IMapper mapper,
        ISettingsManager settingsManager,
        ILogger<ShippingOrderService> logger)
    {
        _ordersWebClient = ordersWebClient;
        _trackingWebClient = trackingWebClient;
        _trackingResponseCacheService = trackingResponseCacheService;
        _mapper = mapper;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public async Task<CreateOrdersResponseModel> CreateOrders(CreateOrderModel[] orders, int practiceId)
    {
        string[] settingsNames = {
            SettingsNames.ShipStation.ApiUrl,
            SettingsNames.ShipStation.ApiKey,
            SettingsNames.ShipStation.ApiSecret
        };
        
        var settings = await _settingsManager.GetSettings(settingsNames, practiceId);
        
        var credentials = new CredentialsModel(
            apiKey: settings[SettingsNames.ShipStation.ApiKey],
            apiSecret: settings[SettingsNames.ShipStation.ApiSecret],
            url: settings[SettingsNames.ShipStation.ApiUrl]);

        _ordersWebClient.Initialize(credentials);
        
        return await _ordersWebClient.CreateOrders(orders);
    }
    
    public async Task<ShipmentModel> GetShipment(int orderId, int practiceId)
    {
        string[] settingsNames = {
            SettingsNames.ShipStation.ApiUrl,
            SettingsNames.ShipStation.ApiKey,
            SettingsNames.ShipStation.ApiSecret
        };
        
        var settings = await _settingsManager.GetSettings(settingsNames, practiceId);
        
        var credentials = new CredentialsModel(
            apiKey: settings[SettingsNames.ShipStation.ApiKey],
            apiSecret: settings[SettingsNames.ShipStation.ApiSecret],
            url: settings[SettingsNames.ShipStation.ApiUrl]);

        _ordersWebClient.Initialize(credentials);
        
        var response = await _ordersWebClient.GetShipments(orderId);

        return _mapper.Map<ShipmentModel>(response.Shipments.FirstOrDefault());
    }

    public async Task<TrackingModel> GetTracking(string carrierCode, string trackingNumber, int practiceId)
    {
        string key = string.Empty;
        string cachedItem = string.Empty;
        var hashCode = (carrierCode + trackingNumber).GetHashCode();
        
        string[] settingsNames = {
            SettingsNames.ShipEngine.ApiUrl,
            SettingsNames.ShipEngine.ApiKey
        };
        
        var settings = await _settingsManager.GetSettings(settingsNames, practiceId);

        try
        {
            key = GenerateKey(nameof(_trackingWebClient.GetTracking), hashCode);

            var cachedResult = await _trackingResponseCacheService.GetAsync(
                key: key,
                getter: async () =>
                {
                    var credentials = new ShipEngine.Clients.Credentials.CredentialsModel(
                        apiKey: settings[SettingsNames.ShipEngine.ApiKey],
                        url: settings[SettingsNames.ShipEngine.ApiUrl]);
                
                    _trackingWebClient.Initialize(credentials);
                
                    return await _trackingWebClient.GetTracking(carrierCode, trackingNumber);
                });
            
            return _mapper.Map<TrackingModel>(cachedResult);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(
                $"Error attempting to get key from cache for [HashCode] = {hashCode} and [Key] = {key} - {ex}");

            throw;
        }
    }

    #region private

    private string GenerateKey(string methodName, int hashCode) {
        return $"{methodName}_${hashCode}";
    }

    #endregion

}
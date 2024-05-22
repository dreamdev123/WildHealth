using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.ShippingOrders;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.SyncRecords;
using WildHealth.Settings;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.ShipStation.Clients.Exceptions;
using WildHealth.ShipStation.Clients.Models;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class UploadDorothyOrdersCommandHandler : IRequestHandler<UploadDorothyOrdersCommand>
{
    private const int MaxOrders = 4000;
    private const int BatchSize = 100;
    
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IShippingOrderService _shippingOrdersService;
    private readonly ITransactionManager _transactionManager;
    private readonly ISettingsManager _settingsManager;
    private readonly ILogger<UploadDorothyOrdersCommandHandler> _logger;
    
    private static readonly string[] SettingNames = {
        SettingsNames.ShipStation.InHouseTagId,
        SettingsNames.ShipStation.ThirdPartyTagId
    };

    public UploadDorothyOrdersCommandHandler(
        ISyncRecordsService syncRecordsService,
        IShippingOrderService shippingOrdersService,
        ITransactionManager transactionManager,
        ISettingsManager settingsManager,
        ILogger<UploadDorothyOrdersCommandHandler> logger)
    {
        _syncRecordsService = syncRecordsService;
        _shippingOrdersService = shippingOrdersService;
        _transactionManager = transactionManager;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public async Task Handle(UploadDorothyOrdersCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Upload of dorothy orders for [PracticeId] = {command.PracticeId} has: started");

        var syncRecordDorothyOrders = await _syncRecordsService.GetByTypeAndStatus<SyncRecordDorothyOrder>(
            type: SyncRecordType.DorothyOrder,
            statuses: new[] { SyncRecordStatus.DorothyOrderCreated },
            count: MaxOrders,
            isTracking: true,
            practiceId: command.PracticeId);

        if (syncRecordDorothyOrders.IsNullOrEmpty())
        {
            _logger.LogInformation($"Upload of dorothy orders for [PracticeId] = {command.PracticeId} has: found no orders to upload");
            
            return;
        }
        
        await LockSyncRecords(syncRecordDorothyOrders, cancellationToken);

        var batches = syncRecordDorothyOrders.Split(BatchSize);
        
        var settings = await _settingsManager.GetSettings(SettingNames, command.PracticeId);
        var inHouseTagId = Convert.ToInt32(settings[SettingsNames.ShipStation.InHouseTagId]);
        var thirdPartyTagId = Convert.ToInt32(settings[SettingsNames.ShipStation.ThirdPartyTagId]);
        
        foreach (var batch in batches)
        {
            try
            {
                var syncRecordOrders = batch.ToArray();
                var orders = syncRecordOrders.Select(o => CreateOrderModel(o, inHouseTagId, thirdPartyTagId)).ToArray();

                var response = await _shippingOrdersService.CreateOrders(orders, command.PracticeId);
                
                foreach (var result in response.Results)
                {
                    var syncRecordDorothyOrder = syncRecordOrders.FirstOrDefault(o => o.OrderNumber == result.OrderNumber);

                    if (syncRecordDorothyOrder is null)
                    {
                        _logger.LogError($"Upload of dorothy orders for [PracticeId] = {command.PracticeId} has: failed to find created order in batch [OrderNumber] = {result.OrderNumber}");
                        continue;
                    }
                    
                    if (result.Success)
                    {
                        syncRecordDorothyOrder.SyncRecord.Status = SyncRecordStatus.DorothyOrderUploaded;
                        syncRecordDorothyOrder.OrderId = result.OrderId;
                        
                        await _syncRecordsService.UpdateAsync(syncRecordDorothyOrder);
                    }
                    else
                    {
                        _logger.LogError($"Upload of dorothy orders for [PracticeId] = {command.PracticeId} has: failed to upload [OrderNumber] = {result.OrderNumber} with error {result.ErrorMessage}");
                        await HandleOrderUploadFailure(syncRecordDorothyOrder);
                    }
                }
            }
            catch (ShipStationException ex)
            {
                _logger.LogError($"Upload of dorothy orders for [PracticeId] = {command.PracticeId} has: failed a batch upload with [Error] = ${ex}");
            }
        }
        
        _logger.LogInformation($"Upload of dorothy orders for [PracticeId] = {command.PracticeId} has: finished");
    }

    #region private

    private async Task LockSyncRecords(SyncRecordDorothyOrder[] orders, CancellationToken cancellationToken)
    {
        await using var transaction = _transactionManager.BeginTransaction();
        
        foreach (var order in orders)
        {
            order.SyncRecord.Status = SyncRecordStatus.Locked;
            await _syncRecordsService.UpdateAsync(order.SyncRecord);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task HandleOrderUploadFailure(SyncRecordDorothyOrder order)
    {
        order.SyncRecord.Status = SyncRecordStatus.DorothyOrderFailed;
        await _syncRecordsService.UpdateAsync(order.SyncRecord);
    }

    private CreateOrderModel CreateOrderModel(SyncRecordDorothyOrder order, int inHouseTagId, int thirdPartyTagId)
    {
        var inHouseStates = new[] { "Kentucky", "KY", "Puerto Rico", "PR" };

        var distributorTagId = (order.Quantity < 4 || inHouseStates.Contains(order.State))
            ? inHouseTagId
            : thirdPartyTagId;
        
        return new CreateOrderModel
        {
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            OrderStatus = ShipStationConstants.OrderStatus.AwaitingShipment,
            BillTo = new AddressModel
            {
                Street1 = order.StreetAddress1,
                Street2 = order.StreetAddress2,
                City = order.City,
                State = order.State,
                ZipCode = order.ZipCode,
                Country = "US",
                Name = order.CustomerName,
                Phone = order.PhoneNumber
            },
            ShipTo = new AddressModel
            {
                Street1 = order.StreetAddress1,
                Street2 = order.StreetAddress2,
                City = order.City,
                State = order.State,
                ZipCode = order.ZipCode,
                Country = "US",
                Name = order.CustomerName,
                Phone = order.PhoneNumber
            },
            CustomerEmail = order.Email,
            Items = new[]
            {
                new OrderItemModel()
                {
                    Sku = order.ItemSku,
                    Name = order.ItemName,
                    Quantity = order.Quantity
                }
            },
            AdvancedOptions = new AdvancedOptionsModel
            {
                StoreId = order.StoreId,
                WarehouseId = order.WarehouseId,
                CustomField1 = order.ClaimUniversalId
            },
            TagIds = new []{ distributorTagId }
        };
    }

    #endregion
}
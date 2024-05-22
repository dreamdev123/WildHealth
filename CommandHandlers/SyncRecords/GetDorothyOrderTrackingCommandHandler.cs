using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Services.ShippingOrders;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Models.ShippingOrders;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Enums.SyncRecords;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class GetDorothyOrderTrackingCommandHandler : IRequestHandler<GetDorothyOrderTrackingCommand, TrackingModel?>
{
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IShippingOrderService _shippingOrdersService;
    private readonly ILogger<GetDorothyOrderTrackingCommandHandler> _logger;

    public GetDorothyOrderTrackingCommandHandler(
        ISyncRecordsService syncRecordsService,
        IShippingOrderService shippingOrderService,
        ILogger<GetDorothyOrderTrackingCommandHandler> logger)
    {
        _syncRecordsService = syncRecordsService;
        _shippingOrdersService = shippingOrderService;
        _logger = logger;
    }

    public async Task<TrackingModel?> Handle(GetDorothyOrderTrackingCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            $"Fetching dorothy order for claim universal id = {command.ClaimUniversalId} has: started");

        var keys = new Dictionary<string, string>()
        {
            { nameof(SyncRecordDorothyOrder.ClaimUniversalId), command.ClaimUniversalId.ToString() }
        };

        var syncRecordDorothyOrder =
            (await _syncRecordsService.GetByKeys<SyncRecordDorothyOrder>(keys)).FirstOrDefault();

        if (syncRecordDorothyOrder is null ||
            syncRecordDorothyOrder.SyncRecord.Status == SyncRecordStatus.DorothyOrderCreated)
        {
            _logger.LogInformation(
                $"Fetching dorothy order for claim universal id = {command.ClaimUniversalId} has: failed to find uploaded sync record");
            return null;
        }

        try
        {
            var shipment = await _shippingOrdersService.GetShipment(
                orderId: syncRecordDorothyOrder.OrderId, 
                practiceId: syncRecordDorothyOrder.SyncRecord.PracticeId);

            if (shipment is null)
            {
                _logger.LogInformation(
                    $"Fetching dorothy order for claim universal id = {command.ClaimUniversalId} has: failed to find shipment for orderId = {syncRecordDorothyOrder.OrderId}");

                return null;
            }
            
            var tracking = await _shippingOrdersService.GetTracking(
                carrierCode: shipment.CarrierCode, 
                trackingNumber: shipment.TrackingNumber, 
                practiceId: syncRecordDorothyOrder.SyncRecord.PracticeId);

            _logger.LogInformation($"Fetching dorothy order for claim universal id = {command.ClaimUniversalId} has: finished");

            return tracking;
        }
        catch (Exception e)
        {
            _logger.LogInformation(
                $"Fetching dorothy order for claim universal id = {command.ClaimUniversalId} has: errored {e}");
        }

        return null;
    }
}
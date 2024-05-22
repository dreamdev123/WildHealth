using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Maps;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.Insurances;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Enums.Insurance;
using WildHealth.Domain.Enums.SyncRecords;
using WildHealth.Lob.Clients.Models;
using WildHealth.Settings;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class CreateDorothyOrderCommandHandler : IRequestHandler<CreateDorothyOrderCommand>
{
    private const string OrderPrefix = "CLM";
    
    private readonly IClaimsService _claimsService;
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IMediator _mediator;
    private readonly ISettingsManager _settingsManager;
    private readonly ILogger<CreateDorothyOrderCommandHandler> _logger;
    
    private static readonly string[] SettingNames = {
        SettingsNames.ShipStation.StoreId,
        SettingsNames.ShipStation.WarehouseId,
        SettingsNames.ShipStation.CovidTestSku,
        SettingsNames.ShipStation.CovidTestName,
        SettingsNames.ShipStation.UnitsPerCovidTestPackage
    };

    public CreateDorothyOrderCommandHandler(
        IClaimsService claimsService, 
        ISyncRecordsService syncRecordsService,
        IMediator mediator,
        ISettingsManager settingsManager, 
        ILogger<CreateDorothyOrderCommandHandler> logger)
    {
        _claimsService = claimsService;
        _syncRecordsService = syncRecordsService;
        _mediator = mediator;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public async Task Handle(CreateDorothyOrderCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Creation of dorothy order for claim [Id] = {command.ClaimId} has: started");

        var claim = await _claimsService.GetById(command.ClaimId);

        var validClaimStatuses = new[] { ClaimStatus.Paid, ClaimStatus.PartiallyPaid };
        if (claim is null || !validClaimStatuses.Contains(claim.ClaimStatus))
        {
            throw new AppException(HttpStatusCode.NotFound,
                $"Creation of dorothy order for claim [Id] = {command.ClaimId} has: failed to find a paid claim");
        }

        var syncRecord = await _syncRecordsService.GetByUniversalId<SyncRecordDorothy>(claim.ClaimantUniversalId);
        
        if (syncRecord is null)
        {
            throw new AppException(HttpStatusCode.NotFound,
                $"Creation of dorothy order for claim [Id] = {command.ClaimId} has: failed to find a sync record");
        }

        var orderNumber = $"{OrderPrefix}{claim.GetId()}";
        
        var keys = new Dictionary<string, string>()
        {
            {nameof(SyncRecordDorothyOrder.OrderNumber), orderNumber }
        };
        var existingSyncRecordDorothyOrder = await _syncRecordsService.GetByKeys<SyncRecordDorothyOrder>(keys);

        if (!existingSyncRecordDorothyOrder.IsNullOrEmpty())
        {
            throw new AppException(HttpStatusCode.BadRequest,
                $"Creation of dorothy order for claim [Id] = {command.ClaimId} has: found an existing order with [OrderNumber] = {orderNumber}");
        }
        
        var verifiedAddress = await VerifyAddress(address: syncRecord.FullAddress);
        
        var settings = await _settingsManager.GetSettings(SettingNames, syncRecord.SyncRecord.PracticeId);
        var storeId = Convert.ToInt32(settings[SettingsNames.ShipStation.StoreId]);
        var warehouseId = Convert.ToInt32(settings[SettingsNames.ShipStation.WarehouseId]);
        var itemSku = settings[SettingsNames.ShipStation.CovidTestSku];
        var itemName = settings[SettingsNames.ShipStation.CovidTestName];
        var unitsPerPackage = Convert.ToInt32(settings[SettingsNames.ShipStation.UnitsPerCovidTestPackage]);

        var syncRecordDorothyOrder = new SyncRecordDorothyOrder
        {
            ClaimUniversalId = claim.UniversalId.ToString(),
            OrderNumber = orderNumber,
            OrderDate = DateTime.UtcNow.ToString("s"),
            StoreId = storeId,
            WarehouseId = warehouseId,
            ItemSku = itemSku,
            ItemName = itemName,
            Quantity = claim.Procedure.Units / unitsPerPackage,
            CustomerName = syncRecord.GetFullname(),
            StreetAddress1 = verifiedAddress?.PrimaryLine ?? syncRecord.StreetAddress1,
            StreetAddress2 = verifiedAddress?.SecondaryLine ?? syncRecord.StreetAddress2,
            City = verifiedAddress?.Components.City ?? syncRecord.City,
            State = verifiedAddress?.Components.State ?? syncRecord.State,
            ZipCode = verifiedAddress?.Components.ZipCode ?? syncRecord.ZipCode,
            PhoneNumber = syncRecord.PhoneNumber,
            Email = syncRecord.Email
        };

        await _syncRecordsService.CreateAsync(
            syncRecord: syncRecordDorothyOrder,
            type: SyncRecordType.DorothyOrder,
            practiceId: syncRecord.SyncRecord.PracticeId,
            status: SyncRecordStatus.DorothyOrderCreated);
    }
    
    #region private

    private async Task<LobVerifyAddressResponseModel?> VerifyAddress(string address)
    {
        try
        {
            return await _mediator.Send(new VerifyAddressCommand(fullAddress: address)); 
        }
        catch(Exception)
        {
            return null;
        }
    }
    
    #endregion
}
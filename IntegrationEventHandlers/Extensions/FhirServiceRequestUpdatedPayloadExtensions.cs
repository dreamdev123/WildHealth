using System.Linq;
using System.Net;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.IntegrationEvents.Fhir.Enums;
using WildHealth.IntegrationEvents.Fhir.Payloads;
using WildHealth.Shared.Exceptions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.IntegrationEventHandlers.Extensions;

public static class FhirServiceRequestUpdatedPayloadExtensions
{
    private static readonly string OrdersBarcodeOrageneSystemName = "kit/orageneBarcode";
    private static readonly string OrdersBarcodeKitSystemName = "kit/barcodeId";
    
    public static async Task Handle(this FhirServiceRequestUpdatedPayload payload, 
        ILogger<FhirServiceRequestIntegrationEventHandler> logger,
        IGeneralRepository<WildHealth.Domain.Entities.Integrations.Integration> integrationsRepository,
        IDnaOrdersService dnaOrdersService,
        IIntegrationsService integrationsService)
    {
        if (payload.Purpose == ServiceRequestPurpose.KitDnaOrder)
        {
            await ProcessFhirServiceRequestUpdatedPayloadKtDnaOrder(payload, logger, integrationsRepository, dnaOrdersService, integrationsService);       
        }
    }
    
    private static async Task ProcessFhirServiceRequestUpdatedPayloadKtDnaOrder(
        FhirServiceRequestUpdatedPayload payload, 
        ILogger<FhirServiceRequestIntegrationEventHandler> logger,
        IGeneralRepository<WildHealth.Domain.Entities.Integrations.Integration> integrationsRepository,
        IDnaOrdersService dnaOrdersService,
        IIntegrationsService integrationsService)
    {
        logger.LogInformation($"Starting handling of Fhir service request [PayloadId] = {payload.Id}");
        
        logger.LogInformation($"Looking for Integration [Vendor] = {IntegrationVendor.Kit}, [Purpose] = {IntegrationPurposes.Order.ExternalId}, [Value] = {payload.Id}");

        // Locate the order associated with this
        var integration =
            await integrationsRepository
                .All()
                .Include(o => o.OrderIntegration).ThenInclude(o => o.Order).ThenInclude(o => o.Integrations).ThenInclude(o => o.Integration)
                .FirstOrDefaultAsync(o => o.Vendor == IntegrationVendor.Kit &&
                                          o.Purpose == IntegrationPurposes.Order.ExternalId &&
                                          o.Value == payload.Id);

        if (integration is null)
        {
            throw new AppException(
                HttpStatusCode.NotFound,
                $"Received Fhir service request with [Id] = {payload.Id} that does not exist in the system");
        }

        if (integration.OrderIntegration.Order is not DnaOrder dnaOrder)
        {
            throw new AppException(
                HttpStatusCode.NotFound,
                $"{integration.OrderIntegration.Order.GetId()} is not a Dna Order");
        }
        
        logger.LogInformation($"Located [DnaOrderId] = {dnaOrder.GetId()}");

        var orageneBarcode = await ParseAndCreateIntegration(
            keyName: OrdersBarcodeOrageneSystemName,
            integrationPurpose: IntegrationPurposes.Order.OrageneExternalId,
            dnaOrder: dnaOrder,
            integrationsService: integrationsService, 
            logger: logger, 
            payload: payload);
        
        // Not doing anything else with this barcode
        await ParseAndCreateIntegration(
            keyName: OrdersBarcodeKitSystemName, 
            integrationPurpose: IntegrationPurposes.Order.KitExternalId, 
            dnaOrder: dnaOrder, 
            integrationsService: integrationsService, 
            logger: logger, 
            payload: payload);

        var status = GetOrderStatus(payload: payload);

        if (dnaOrder is not null && orageneBarcode is not null)
        {
            dnaOrder.UpdateDnaOrderInformation(orageneBarcode);

            await dnaOrdersService.UpdateAsync(dnaOrder);
            
            logger.LogInformation($"Successfully assigned [OrageneBarcode] = {orageneBarcode} to Dna Order [Id] = {dnaOrder.GetId()}");
        }

        if (dnaOrder is not null && status.HasValue)
        {
            dnaOrder.UpdateStatus(status.Value);

            await dnaOrdersService.UpdateAsync(dnaOrder);
            
            logger.LogInformation($"Successfully assigned [Status] = {status} to Dna Order [Id] = {dnaOrder.GetId()}");
        }
        
        logger.LogInformation($"Finished handling of Fhir service request [PayloadId] = {payload.Id}");
    }

    private static async Task<string?> ParseAndCreateIntegration(string keyName, string integrationPurpose, DnaOrder dnaOrder, IIntegrationsService integrationsService, ILogger logger, FhirServiceRequestUpdatedPayload payload)
    {
        var value =  payload.Identifiers.IsNullOrEmpty() ? null : payload.Identifiers.FirstOrDefault(o => o.System == keyName)?.Value;

        logger.LogInformation($"Searched for [Identifier] = {keyName} and received [Value] = {value}");
        
        if (value is not null)
        {
            await CreateOrUpdateIntegration(
                dnaOrder: dnaOrder,
                integrationPurpose: integrationPurpose,
                integrationVendor: IntegrationVendor.Kit,
                value: value, 
                integrationsService: integrationsService);
            
            logger.LogInformation($"Created OrderIntegration with [Purpose] = {integrationPurpose}, [Vendor] = {IntegrationVendor.Kit}, [Value] = {value}");
        }
        
        return value;
    }

    private static OrderStatus? GetOrderStatus(FhirServiceRequestUpdatedPayload payload)
    {
        if (payload.OrderDetail is null || !payload.OrderDetail.Any())
        {
            return null;
        }

        return payload.OrderDetail.First().Text switch
        {
            "DNA_KIT_CUSTOMER_ORDERED" => OrderStatus.Placed,
            "DNA_KIT_WAREHOUSE_OUTGOING" => OrderStatus.Shipping,
            "DNA_KIT_SHIPPED_TO_CUSTOMER" => OrderStatus.OutForDelivery,
            "DNA_KIT_CUSTOMER_RECEIVED" => OrderStatus.Arrived,
            "DNA_KIT_CUSTOMER_SHIPPED" => OrderStatus.ReturnProcessing,
            "DNA_KIT_LABCORP_RECEIVED" => OrderStatus.ReturnArrived,
            _ => null
        };
    }
    
    private static async Task CreateOrUpdateIntegration(
            DnaOrder dnaOrder,
            string integrationPurpose,
            IntegrationVendor integrationVendor,
            string  value,
            IIntegrationsService integrationsService
        )
    {
        var existingIntegration = dnaOrder.Integrations.Select(o => o.Integration)
            .FirstOrDefault(o => o.Vendor == integrationVendor && o.Purpose == integrationPurpose);

        if (existingIntegration is null)
        {
            var orderOrageneIntegration = new OrderIntegration(
                order: dnaOrder,
                purpose: integrationPurpose,
                vendor: integrationVendor,
                value: value);
        
            await integrationsService.CreateAsync(orderOrageneIntegration);

            return;
        }

        existingIntegration.Value = value;

        await integrationsService.UpdateAsync(existingIntegration);
    }
}
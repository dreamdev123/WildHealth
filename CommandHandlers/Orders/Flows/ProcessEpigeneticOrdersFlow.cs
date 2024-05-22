using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.EpigeneticOrders;
using WildHealth.IntegrationEvents.EpigeneticOrders.Payloads;
using WildHealth.TrueDiagnostic.Models;

namespace WildHealth.Application.CommandHandlers.Orders.Flows;

public record ProcessEpigeneticOrdersFlow(
    EpigeneticOrder Order, 
    OrderStatusModel StatusModel,
    DateTime UtcNow) : IMaterialisableFlow
{
    private readonly IDictionary<(string orderStaus, string shippingStatus, string kitStatus), OrderStatus> _transitionMatrix = 
        new Dictionary<(string orderStaus, string shippingStatus, string kitStatus), OrderStatus>
    {
        // Empty string status means - any
        { ("Order Received", "Pending", ""), OrderStatus.Placed },
        { ("Kit Id Generated", "Pending", "Not Registered"), OrderStatus.Placed },
        { ("Shipped", "Shipped", "Not Registered"), OrderStatus.Shipping },
        { ("Delivered", "Delivered", "Registered"), OrderStatus.Arrived },
        { ("Delivered", "Delivered", "Results Ready"), OrderStatus.Completed },
    };
    
    public MaterialisableFlowResult Execute()
    {
        var correspondingStatus = GetCorrespondingOrderStatus();

        if (!AnyActionsRequired(correspondingStatus))
        {
            return MaterialisableFlowResult.Empty;
        }
        
        return correspondingStatus switch
        {
            OrderStatus.Placed => MaterialisableFlowResult.Empty,
            OrderStatus.Shipping => MarkOrderAsShipped(),
            OrderStatus.Arrived => MarkOrderAsArrived(),
            OrderStatus.Completed => FireOrderResultedEvent(),
            
            _ => MaterialisableFlowResult.Empty
        };
    }
    
    #region private

    private OrderStatus? GetCorrespondingOrderStatus()
    {
        var kitStatus = StatusModel.SampleKits.FirstOrDefault()?.Status ?? string.Empty;
        
        if (!_transitionMatrix.ContainsKey((StatusModel.OrderStatus, StatusModel.ShippingStatus, kitStatus)))
        {
            return null;
        }

        return _transitionMatrix[(StatusModel.OrderStatus, StatusModel.ShippingStatus, kitStatus)];
    }

    private bool AnyActionsRequired(OrderStatus? correspondingStatus)
    {
        if (correspondingStatus is null)
        {
            return false;
        }
        
        return Order.Status != correspondingStatus;
    }

    private MaterialisableFlowResult MarkOrderAsShipped()
    {
        Order.ChangeStatus(OrderStatus.Shipping, UtcNow);
        
        return Order.Updated();
    }
    
    private MaterialisableFlowResult MarkOrderAsArrived()
    {
        Order.ChangeStatus(OrderStatus.Arrived, UtcNow);
        
        return Order.Updated();
    }
    
    private MaterialisableFlowResult FireOrderResultedEvent()
    {
        var @event = new EpigeneticOrderIntegrationEvent(
            payload: new EpigeneticOrderResultedPayload(
                orderId: Order.GetId(),
                orderNumber: Order.Number
            ),
            patient: new PatientMetadataModel(
                id: Order.PatientId,
                universalId: Order.Patient.UniversalId.ToString()
            ),
            eventDate: UtcNow
        );

        return MaterialisableFlowResult.Empty + @event;
    }
    
    #endregion
}
using System;
using System.Linq;
using System.Collections.Generic;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Orders;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.AddOns;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Exceptions;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Orders.Flows;

public class CreateOtherOrderFlow : IMaterialisableFlow
{
    private readonly CreateOtherOrderItemModel[] _itemModels;
    private readonly OtherOrderDataModel _dataModel;
    private readonly string _patientProfileLink;
    private readonly bool _sendForReview;
    private readonly bool _isCompleted;
    private readonly Employee _employee;
    private readonly Patient _patient;
    private readonly AddOn[] _addOns;
    private readonly int _employeeId;
    private readonly DateTime _utcNow;

    public CreateOtherOrderFlow(
        CreateOtherOrderItemModel[] itemModels, 
        OtherOrderDataModel dataModel, 
        string patientProfileLink,
        bool sendForReview,
        bool isCompleted,
        Employee employee,
        Patient patient, 
        AddOn[] addOns, 
        int employeeId,
        DateTime utcNow)
    {
        _patientProfileLink = patientProfileLink;
        _sendForReview = sendForReview;
        _isCompleted = isCompleted;
        _itemModels = itemModels;
        _dataModel = dataModel;
        _employee = employee;
        _patient = patient;
        _addOns = addOns;
        _employeeId = employeeId;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        AssertAddOnsType(_addOns);

        var orderNumber = Guid.NewGuid().ToString().Substring(0, 8);

        var provider = _addOns.FirstOrDefault()?.Provider ?? AddOnProvider.Undefined;
        
        var orderItems = CreateOrderItems(_addOns, _itemModels);
        
        var data = CreateOrderData(_dataModel);
        
        var order = new OtherOrder(
            patient: _patient,
            employee: _employee,
            number: orderNumber,
            items: orderItems,
            data: data,
            provider: provider,
            date: _utcNow
        );

        if (_sendForReview)
        {
            order.ReviewedBy = _employee;
            
            order.SendForReview(_utcNow);
        }
        
        if (_isCompleted)
        {
            if (_employeeId != order.ReviewedBy.GetId())
            {
                throw new DomainException("You can not complete the order");
            }
            
            order.ReviewedBy = _employee;
            
            order.Sign(_utcNow);
        }

        return order.Added() + new OrderCreatedEvent(order) + RaiseNotificationIfOrderCompleted(order);
    }
    
    #region private
    
    /// <summary>
    /// Asserts if add-on types matches with order type
    /// </summary>
    /// <param name="addOns"></param>
    /// <exception cref="AppException"></exception>
    private void AssertAddOnsType(AddOn[] addOns)
    {
        if (addOns.Any(x => x.OrderType != OrderType.Other))
        {
            throw new DomainException("Add on type and order type does not match.");
        }
    }

    /// <summary>
    /// Creates and returns order items based on add-ons
    /// </summary>
    /// <param name="addOns"></param>
    /// <param name="itemModels"></param>
    /// <returns></returns>
    private OrderItem[] CreateOrderItems(AddOn[] addOns, CreateOtherOrderItemModel[] itemModels)
    {
        return addOns.Select(addOn =>
        {
            var item = new OrderItem(addOn);

            var description = itemModels.FirstOrDefault(x => x.AddOnId == addOn.GetId())?.Description;

            item.FillOut(
                sku: addOn.IntegrationId,
                description: addOn.Name + (!string.IsNullOrEmpty(description) ? " — " + description : string.Empty),
                price: addOn.GetPrice(),
                quantity: 1
            );

            return item;
        }).ToArray();
    }

    /// <summary>
    /// Creates order data based on order data model
    /// </summary>
    /// <param name="dataModel"></param>
    /// <returns></returns>
    private OrderData[] CreateOrderData(OtherOrderDataModel dataModel)
    {
        var data = new List<OrderData>();

        var type = dataModel.GetType();
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(dataModel, null)?.ToString();
            
            data.Add(new OrderData
            {
                Key = property.Name,
                Value = value
            });
        }

        return data.ToArray();
    }

    private MaterialisableFlowResult RaiseNotificationIfOrderCompleted(Order order) => _isCompleted
        ? new NewDiagnosticOrderNotification(order, _employee, _patient, _patientProfileLink).ToFlowResult()
        : MaterialisableFlowResult.Empty;

    #endregion
}
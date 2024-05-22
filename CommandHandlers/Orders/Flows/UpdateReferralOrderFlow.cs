using System;
using System.Linq;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Orders;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.AddOns;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Exceptions;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Orders.Flows;

public class UpdateReferralOrderFlow : IMaterialisableFlow
{
    private readonly CreateReferralOrderItemModel[] _itemModels;
    private readonly ReferralOrderDataModel _dataModel;
    private readonly string _patientProfileLink;
    private readonly bool _sendForReview;
    private readonly bool _isCompleted;
    private readonly ReferralOrder _order;
    private readonly Employee? _employee;
    private readonly AddOn[] _addOns;
    private readonly int _employeeId;
    private readonly DateTime _utcNow;

    public UpdateReferralOrderFlow(
        CreateReferralOrderItemModel[] itemModels, 
        ReferralOrderDataModel dataModel, 
        string patientProfileLink,
        bool sendForReview, 
        bool isCompleted,
        ReferralOrder order, 
        Employee? employee, 
        AddOn[] addOns, 
        int employeeId,
        DateTime utcNow)
    {
        _patientProfileLink = patientProfileLink;
        _itemModels = itemModels;
        _dataModel = dataModel;
        _sendForReview = sendForReview;
        _isCompleted = isCompleted;
        _order = order;
        _employee = employee;
        _addOns = addOns;
        _employeeId = employeeId;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        AssertOrderCanBeUpdated(_order);
        
        AssertAddOnsType(_addOns);
        
        var provider = _addOns.FirstOrDefault()?.Provider ?? AddOnProvider.Undefined;
        
        _order.OverwriteItems(CreateOrderItems(_addOns, _itemModels), provider);
        
        _order.OverwriteData(CreateOrderData(_dataModel));
        
        if (_sendForReview)
        {
            _order.ReviewedBy = _employee;
            
            _order.SendForReview(_utcNow);
        }
        
        if (_isCompleted)
        {
            if (_employeeId != _employee?.GetId())
            {
                throw new DomainException("You can not complete the order, the only person that can update this order is the creator of the order");
            }
            
            _order.ReviewedBy = _employee;

            _order.Sign(_utcNow);
        }

        return _order.Updated() + RaiseNotificationIfOrderCompleted(_order);
    }

    #region private
    
    /// <summary>
    /// Asserts order can be modified
    /// </summary>
    /// <param name="order"></param>
    /// <exception cref="DomainException"></exception>
    private void AssertOrderCanBeUpdated(ReferralOrder order)
    {
        if (order.Status > OrderStatus.UnderReview)
        {
            throw new DomainException("Order can't me modified.");
        }
    }
    
    /// <summary>
    /// Asserts if add-on types matches with order type
    /// </summary>
    /// <param name="addOns"></param>
    /// <exception cref="AppException"></exception>
    private void AssertAddOnsType(AddOn[] addOns)
    {
        if (addOns.Any(x => x.OrderType != OrderType.Referral))
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
    private OrderItem[] CreateOrderItems(AddOn[] addOns, CreateReferralOrderItemModel[] itemModels)
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
    private OrderData[] CreateOrderData(ReferralOrderDataModel dataModel)
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
        ? new NewDiagnosticOrderNotification(order, _employee, order.Patient, _patientProfileLink).ToFlowResult()
        : MaterialisableFlowResult.Empty;
    
    #endregion
}
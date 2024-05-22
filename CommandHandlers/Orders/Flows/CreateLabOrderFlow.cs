using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.AddOns;

namespace WildHealth.Application.CommandHandlers.Orders.Flows;

public class CreateLabOrderFlow : IMaterialisableFlow
{
    private readonly Patient _patient;
    private readonly string _number;
    private readonly OrderItem[] _orderItems;
    private readonly AddOnProvider _addOnsProvider;
    private readonly DateTime _utcNow;

    public CreateLabOrderFlow(Patient patient, string number, OrderItem[] orderItems, AddOnProvider addOnsProvider, DateTime utcNow)
    {
        _patient = patient;
        _number = number;
        _orderItems = orderItems;
        _addOnsProvider = addOnsProvider;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        var order = MakeOrder(_patient, _number, _orderItems, _addOnsProvider, _utcNow);

        return order.Added().ToFlowResult();
    }

    private LabOrder MakeOrder(
        Patient patient, 
        string orderNumber, 
        OrderItem[] orderItems, 
        AddOnProvider provider,
        DateTime date)
    {
        if (string.IsNullOrEmpty(orderNumber))
        {
            return new LabOrder(
                patient: patient,
                items: orderItems,
                provider: provider,
                date: date
            );
        }

        return new LabOrder(
            patient: patient,
            number: orderNumber,
            items: orderItems,
            provider: provider,
            date: date
        );
    }
}
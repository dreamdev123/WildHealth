using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public class CreateUpfrontSubscriptionFlow : IMaterialisableFlow
{
    private readonly Patient _patient;
    private readonly PaymentPrice _paymentPrice;
    private readonly DateTime _startDate;

    public CreateUpfrontSubscriptionFlow(
        Patient patient,
        PaymentPrice paymentPrice,
        DateTime startDate)
    {
        _patient = patient;
        _paymentPrice = paymentPrice;
        _startDate = startDate;
    }

    public MaterialisableFlowResult Execute()
    {
        var endDate = _startDate.AddMonths(_paymentPrice.PaymentPeriod.PeriodInMonths);
            
        var subscription = new Subscription(
            paymentPrice: _paymentPrice,
            patient: _patient,
            startDate: _startDate,
            endDate: endDate);

        return subscription.Added();
    }
}
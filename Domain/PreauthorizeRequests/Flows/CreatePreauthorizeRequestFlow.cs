using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Flows;

public record CreatePreauthorizeRequestFlow(
    User User, 
    PaymentPlan? PaymentPlan,
    PaymentPeriod? PaymentPeriod,
    PaymentPrice? PaymentPrice,
    EmployerProduct? EmployerProduct) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (PaymentPlan is null)
        {
            throw new DomainException("Payment Plan does not exist");
        }
        
        if (PaymentPeriod is null)
        {
            throw new DomainException("Payment Period does not exist");
        }
        
        if (PaymentPrice is null)
        {
            throw new DomainException("Payment Price does not exist");
        }
        
        var request = new PreauthorizeRequest
        {
            UserId = User.GetId(),
            Token = Guid.NewGuid().ToString(),
            PaymentPlanId = PaymentPlan.GetId(),
            PaymentPeriodId = PaymentPeriod.GetId(),
            PaymentPriceId = PaymentPrice.GetId(),
            EmployerProductId = EmployerProduct?.GetId() ?? null
        };

        return request.Added();
    }
}
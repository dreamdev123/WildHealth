using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Flows;

public record UpdatePreauthorizeRequestFlow(
    PreauthorizeRequest Request, 
    PaymentPlan? PaymentPlan,
    PaymentPeriod? PaymentPeriod,
    PaymentPrice? PaymentPrice,
    EmployerProduct? EmployerProduct) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (Request.IsCompleted || Request.User.IsRegistrationCompleted)
        {
            throw new DomainException("Can't delete completed Preauthorize Request");
        }
        
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
        
        Request.PaymentPlanId = PaymentPlan.GetId();
        Request.PaymentPeriodId = PaymentPeriod.GetId();
        Request.PaymentPriceId = PaymentPrice.GetId();
        Request.EmployerProductId = EmployerProduct?.GetId();

        return Request.Updated();
    }
}
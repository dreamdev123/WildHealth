using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Flows;

public record DeletePreauthorizeRequestFlow(PreauthorizeRequest Request) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (Request.IsCompleted || Request.User.IsRegistrationCompleted)
        {
            throw new DomainException("Can't delete completed Preauthorize Request");
        }

        return Request.Deleted();
    }
}
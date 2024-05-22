using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.CommandHandlers.Auth.Flows;

public class CheckIfEmailInUseCommandFlow
{
    private readonly UserIdentity _identity;

    public CheckIfEmailInUseCommandFlow(UserIdentity identity)
    {
        _identity = identity;
    }

    public CheckIfEmailInUseCommandFlowResult Execute()
    {
        if (_identity == null)
        {
            return new CheckIfEmailInUseCommandFlowResult(false);
        }
        if (_identity.User.IsRegistrationCompleted)
        {
            return new CheckIfEmailInUseCommandFlowResult(true);
        }

        if (_identity.Type == UserType.Employee)
        {
            return new CheckIfEmailInUseCommandFlowResult(true);
        }

        if (_identity.User.Patient?.Options?.IsFellow == true)
        {
            return new CheckIfEmailInUseCommandFlowResult(false);
        }
        
        return new CheckIfEmailInUseCommandFlowResult(true);
    }
}

public record CheckIfEmailInUseCommandFlowResult(bool InUse);
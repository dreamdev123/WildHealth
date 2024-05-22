using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Extensions;

public static class PreauthorizeRequestExtensions
{
    public static string GeneratePersonalRegistrationLink(this PreauthorizeRequest request, string signUpUrlTemplate, string appUrl)
    {
        return string.Format(signUpUrlTemplate, appUrl, request.Token);
    }
}
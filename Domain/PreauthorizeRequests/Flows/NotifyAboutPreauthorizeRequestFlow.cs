using System;
using System.Linq;
using WildHealth.Application.Domain.PreauthorizeRequests.Extensions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.EmployerProducts;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Flows;

public record NotifyAboutPreauthorizeRequestFlow(
    PreauthorizeRequest[] Requests, 
    EmployerProduct[] EmployerProducts,
    DateTime UtcNow,
    string AppUrl,
    string SignUpUrl) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var result = MaterialisableFlowResult.Empty;

        foreach (var request in Requests)
        {
            if (request.IsCompleted || request.User.IsRegistrationCompleted)
            {
                continue;
            }
            
            request.EmailSentAt = UtcNow;

            var employerProduct = EmployerProducts.FirstOrDefault(x => x.Id == request.EmployerProductId);

            var registrationLink = request.GeneratePersonalRegistrationLink(SignUpUrl, AppUrl);

            var customEmailWording = employerProduct?.GetSettings(nameof(EmployerProductSettingsModel.CustomEmailWording));
            
            result += new PreauthorizeRequestNotification(
                user: request.User, 
                registrationLink: registrationLink,
                customWording: customEmailWording
            );

            result += request.Updated();
        }

        return result;
    }
}
using System;
using System.Collections.Generic;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.Utils.AgreementsDataProvider;

/// <summary>
/// <see cref="IAgreementDataProvider"/>
/// </summary>
public class CoachingClientSingleAgreementDataProvider : AgreementDataProviderBase, IAgreementDataProvider
{
    /// <summary>
    /// <see cref="IAgreementDataProvider.GetData"/>
    /// </summary>
    /// <param name="fullName"></param>
    /// <param name="email"></param>
    /// <param name="billingAddress"></param>
    /// <param name="ipAddress"></param>
    /// <param name="planName"></param>
    /// <param name="periodInMonth"></param>
    /// <param name="paymentStrategy"></param>
    /// <param name="date"></param>
    /// <returns></returns>
    public IDictionary<string, string> GetData(
        string fullName, 
        string email,
        string billingAddress,
        string ipAddress,
        string planName,
        int periodInMonth,
        PaymentStrategy paymentStrategy,
        DateTime date)
    {
        var documentReferences = GetDocumentReferences();
        
        return new Dictionary<string, string>
        {
            {"untitled1", documentReferences},
            {"untitled2", documentReferences},
            {"untitled3", documentReferences},
            {"untitled4", documentReferences},
            {"untitled5", documentReferences},
            {"untitled6", documentReferences},
            {"untitled7", documentReferences},
            {"untitled8", documentReferences},
            {"untitled9", Unchecked},
            {"untitled10", Unchecked},
            {"untitled11", Unchecked},
            {"untitled12", Unchecked},
            {"untitled13", fullName},
            {"untitled14", email},
            {"untitled15", ipAddress},
            {"untitled16", date.ToString("d")},
            {"untitled17", "7"}
        };
    }
}
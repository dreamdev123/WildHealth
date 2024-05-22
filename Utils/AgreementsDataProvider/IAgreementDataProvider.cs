using System;
using System.Collections.Generic;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.Utils.AgreementsDataProvider
{
    /// <summary>
    /// Contains method for providing agreement data
    /// </summary>
    public interface IAgreementDataProvider
    {
        /// <summary>
        /// Returns dictionary data for inserting into pdf
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
        IDictionary<string, string> GetData(
            string fullName, 
            string email,
            string billingAddress,
            string ipAddress,
            string planName,
            int periodInMonth,
            PaymentStrategy paymentStrategy,
            DateTime date);
    }
}
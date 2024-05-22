using System;
using System.Collections.Generic;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.Utils.AgreementsDataProvider
{
    /// <summary>
    /// <see cref="IAgreementDataProvider"/>
    /// </summary>
    public class PatientFoundersAgreementDataProvider : AgreementDataProviderBase, IAgreementDataProvider
    {
        private static readonly IDictionary<string, IDictionary<int, IDictionary<PaymentStrategy, string>>> PaymentPriceCheckboxes = new Dictionary<string, IDictionary<int, IDictionary<PaymentStrategy, string>>>
        {
            {
                "FOUNDERS_ADVANCED",
                new Dictionary<int, IDictionary<PaymentStrategy, string>>
                {
                    {
                        12,
                        new Dictionary<PaymentStrategy, string>
                        {
                            { PaymentStrategy.FullPayment, "untitled13" },
                            { PaymentStrategy.PartialPayment, "untitled13" }
                        }
                    },
                }
            },
            {
                "FOUNDERS_OPTIMIZATION",
                new Dictionary<int, IDictionary<PaymentStrategy, string>>
                {
                    {
                        12,
                        new Dictionary<PaymentStrategy, string>
                        {
                            { PaymentStrategy.FullPayment, "untitled14" },
                            { PaymentStrategy.PartialPayment, "untitled14" }
                        }
                    }
                }
            }
        };
        
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
                {"untitled9", documentReferences},
                {"untitled10", DeterminePaymentPeriodCheckBox("untitled10", planName, periodInMonth, paymentStrategy)},
                {"untitled11", DeterminePaymentPeriodCheckBox("untitled11", planName, periodInMonth, paymentStrategy)},
                {"untitled12", DeterminePaymentPeriodCheckBox("untitled12", planName, periodInMonth, paymentStrategy)},
                {"untitled13", DeterminePaymentPeriodCheckBox("untitled13", planName, periodInMonth, paymentStrategy)},
                {"untitled14", DeterminePaymentPeriodCheckBox("untitled14", planName, periodInMonth, paymentStrategy)},
                {"untitled15", DeterminePaymentPeriodCheckBox("untitled15", planName, periodInMonth, paymentStrategy)},
                {"untitled16", fullName},
                {"untitled17", email},
                {"untitled18", ipAddress},
                {"untitled19", date.ToString("d")},
                {"untitled20", "8"},
            };
        }
        
        #region private

        private static string DeterminePaymentPeriodCheckBox(
            string fieldName, 
            string planName,
            int periodInMonth,
            PaymentStrategy paymentStrategy)
        {
            if (!PaymentPriceCheckboxes.ContainsKey(planName.ToUpper()))
            {
                return Unchecked;
            }

            if (!PaymentPriceCheckboxes[planName].ContainsKey(periodInMonth))
            {
                return Unchecked;
            }

            if (!PaymentPriceCheckboxes[planName][periodInMonth].ContainsKey(paymentStrategy))
            {
                return Unchecked;
            }
            
            return PaymentPriceCheckboxes[planName][periodInMonth][paymentStrategy] == fieldName
                ? Checked
                : Unchecked;
        }

        #endregion
    }
}
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Agreements;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.Utils.AgreementFactory
{
    /// <summary>
    /// Creates filled out and signed agreement documents
    /// </summary>
    public interface IAgreementFactory
    {
        /// <summary>
        /// Creates and returns filled out and signed agreement documents
        /// </summary>
        /// <param name="agreement"></param>
        /// <param name="patient"></param>
        /// <param name="ipAddress"></param>
        /// <param name="planName"></param>
        /// <param name="periodInMonth"></param>
        /// <param name="paymentStrategy"></param>
        /// <returns></returns>
        Task<byte[]> CreateAsync(
            Agreement agreement,
            Patient patient,
            string ipAddress,
            string planName,
            int periodInMonth,
            PaymentStrategy paymentStrategy);
    }
}
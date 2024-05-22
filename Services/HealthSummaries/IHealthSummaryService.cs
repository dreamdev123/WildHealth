using System.Threading.Tasks;
using WildHealth.Domain.Entities.HealthSummaries;

namespace WildHealth.Application.Services.HealthSummaries
{
    public interface IHealthSummaryService
    {
        /// <summary>
        /// Returns Health Summary Map
        /// </summary>
        /// <returns></returns>
        Task<HealthSummaryMap[]> GetMapAsync();

        /// <summary>
        /// Returns Health Summary Map filtering by map key 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<HealthSummaryMap[]> GetMapByKeyAsync(string key);
        
        /// <summary>
        /// Returns patients Health Summaries
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<HealthSummaryValue[]> GetByPatientAsync(int patientId);

        /// <summary>
        /// Returns patients Health Summaries
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<HealthSummaryValue> GetByKeyAsync(int patientId, string key);

        /// <summary>
        /// Creates and returns new health summary
        /// </summary>
        /// <param name="healthSummaryValue"></param>
        /// <returns></returns>
        Task<HealthSummaryValue> CreateAsync(HealthSummaryValue healthSummaryValue);

        /// <summary>
        /// Creates batch of health summaries
        /// </summary>
        /// <param name="healthSummaries"></param>
        /// <returns></returns>
        Task<HealthSummaryValue[]> CreateBatchAsync(HealthSummaryValue[] healthSummaries);

        /// <summary>
        /// Updates existing health summary 
        /// </summary>
        /// <param name="healthSummaryValue"></param>
        /// <returns></returns>
        Task<HealthSummaryValue> UpdateAsync(HealthSummaryValue healthSummaryValue);

        /// <summary>
        /// Deletes existing health summary
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task DeleteAsync(int patientId, string key);
    }
}
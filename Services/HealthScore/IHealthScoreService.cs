using System.Threading.Tasks;
using WildHealth.ClarityCore.Models.HealthScore;

namespace WildHealth.Application.Services.HealthScore
{
    public interface IHealthScoreService
    {
        /// <summary>
        /// Return is health score available
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public Task<bool> IsHealthScoreAvailableAsync(int patientId);
        
        /// <summary>
        /// Returns patient health score 
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<HealthScoreResponseModel> GetPatientHealthScore(string patientId);

        /// <summary>
        /// Run patient health score calculation
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<HealthScoreResponseModel> RunPatientHealthScore(string patientId);
    }
}

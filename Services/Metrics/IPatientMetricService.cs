using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.Services.Metrics
{
    /// <summary>
    /// Represents service for interacting with PatientMetrics
    /// </summary>
    public interface IPatientMetricService
    {
        /// <summary>
        /// Creates PatientMetric records for the list of records provided
        /// </summary>
        public Task<List<PatientMetric>> CreateAsync(List<PatientMetric> patientMetrics);

        /// <summary>
        /// Returns a list of patient metrics for a given patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<PatientMetric[]> GetByPatientIdAsync(int patientId);

        /// Gets all patient metrics for the given metrics
        /// </summary>
        public Task<List<PatientMetric>> FindAsync(int patientId, List<Metric> metrics);

        /// <summary>
        /// Get the most recent patient metric for the provided metric
        /// </summary>
        public Task<PatientMetric> GetLatestAsync(int patientID, Metric metric);
    }
}
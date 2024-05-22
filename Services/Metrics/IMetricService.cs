using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.Services.Metrics
{
    /// <summary>
    /// Represents service for interacting with Metrics
    /// </summary>
    public interface IMetricService
    {
        /// <summary>
        /// Gets all of the metrics with the provided source
        /// </summary>
        public Task<Metric[]> GetBySource(MetricSource source);
    }
}
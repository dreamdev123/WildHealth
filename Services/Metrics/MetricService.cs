using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Metrics;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Metrics
{
    /// <summary>
    /// <see cref="IMetricService" />
    /// </summary>
    public class MetricService : IMetricService
    {
        private readonly IGeneralRepository<Metric> _metricsRepository;

        public MetricService(
            IGeneralRepository<Metric> metricsRepository
        )
        {
            _metricsRepository = metricsRepository;
        }

        /// <summary>
        /// <see cref "IMetricService.GetBySource"/>
        /// <summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public async Task<Metric[]> GetBySource(MetricSource source)
        {
            var metrics = await _metricsRepository
                .All()
                .BySource(source)
                .IncludeClassificationType()
                .ToArrayAsync();
            
            if (metrics is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(source), source);
                throw new AppException(HttpStatusCode.NotFound, "Metrics not found for source", exceptionParam);
            }

            return metrics;
        }
    }
}
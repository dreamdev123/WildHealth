using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Metrics;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Metrics
{
    /// <summary>
    /// <see cref="IPatientMetricService" />
    /// </summary>
    public class PatientMetricService : IPatientMetricService
    {
        private readonly IGeneralRepository<PatientMetric> _patientMetricsRepository;

        public PatientMetricService(
            IGeneralRepository<PatientMetric> patientMetricsRepository
        )
        {
            _patientMetricsRepository = patientMetricsRepository;
        }

        /// <summary>
        /// <see cref "IPatientMetricService.CreateAsync"/>
        /// <summary>
        /// <param name="patientMetrics"></param>
        /// <returns></returns>
        public async Task<List<PatientMetric>> CreateAsync(List<PatientMetric> patientMetrics)
        {
            await _patientMetricsRepository.AddRangeAsync(patientMetrics.ToArray());
            await _patientMetricsRepository.SaveAsync();

            return patientMetrics;
        }

        public async Task<PatientMetric[]> GetByPatientIdAsync(int patientId)
        {
            var result = await _patientMetricsRepository
                .All()
                .Where(pm => pm.PatientId == patientId)
                .Include(pm => pm.Metric)
                .Include(x => x.ClassificationTypeOption)
                .OrderByDescending(pm => pm.CreatedAt)
                .ToArrayAsync();

            return result;
        }
        
        
        /// <summary>
        /// <see cref="IPatientMetricService.FindAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="metrics"></param>
        /// <returns></returns>
        public async Task<List<PatientMetric>> FindAsync(int patientId, List<Metric> metrics)
        {
            var results = await _patientMetricsRepository
                .All()
                .ByPatient(patientId)
                .ByMetrics(metrics)
                .ToListAsync();
            
            return results;
        }

        /// <summary>
        /// <see cref="IPatientMetricService.GetLatestAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="metric"></param>
        // <returns></returns>
        public async Task<PatientMetric> GetLatestAsync(int patientId, Metric metric)
        {
            var result = await _patientMetricsRepository
                .All()
                .Include(x => x.ClassificationTypeOption)
                .ByPatient(patientId)
                .ByMetric(metric)
                .OrderByDescending(x => x.CreatedAt)
                .Take(1)
                .FindAsync();

            if (result is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Could not find PatientMetric for PatientId={patientId}, MetricId={metric.Id}");
            }
            
            return result;
        }
    }
}
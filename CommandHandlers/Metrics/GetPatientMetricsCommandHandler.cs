using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using MediatR;
using WildHealth.Application.Commands.Metrics;
using WildHealth.Application.Services.Metrics;
using WildHealth.Common.Models._Base;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Metrics;
namespace WildHealth.Application.CommandHandlers.Metrics
{
    public class GetPatientMetricsCommandHandler : IRequestHandler<GetPatientMetricsCommand, IList<PatientMetric>>
    {
        private readonly IPatientMetricService _patientMetricService;
        private readonly IMediator _mediator;

        public GetPatientMetricsCommandHandler(
            IPatientMetricService patientMetricService,
            IMediator mediator
        )
        {
            _patientMetricService = patientMetricService;
            _mediator = mediator;
        }

        public async Task<IList<PatientMetric>> Handle (GetPatientMetricsCommand command, CancellationToken cancellationToken)
        {
            var patientMetrics = await _patientMetricService.GetByPatientIdAsync(command.PatientId);

            patientMetrics = ApplyMetricSourceFilter(patientMetrics, command.RequestModel.MetricSources);

            var finalPatientMetrics = ApplyOptions(patientMetrics.ToList(), command.RequestModel.Options);
            return finalPatientMetrics;
        }

        private IList<PatientMetric> ApplyOptions(IList<PatientMetric> patientMetrics, KeyValueStringRequestModel[] options)
        {
            foreach(var option in options)
            {
                patientMetrics = option.Key switch
                {
                    MetricConstants.GetPatientMetricOptions.LatestOnly => ApplyLatestOnly(patientMetrics, option.Value),
                    _ => patientMetrics
                };
            }

            return patientMetrics;
        }

        private PatientMetric[] ApplyMetricSourceFilter(PatientMetric[] patientMetrics, MetricSource[] metricSources)
        {
            if(metricSources is not null && metricSources.Any())
            {
                return patientMetrics.Where(x => metricSources.Contains(x.Metric.Source)).ToArray();
            }

            return patientMetrics;
        }

        private IList<PatientMetric> ApplyLatestOnly(IList<PatientMetric> patientMetrics, string shouldApply)
        {
            if (Convert.ToBoolean(shouldApply) == true)
            {
                var patientMetricsByMetric = patientMetrics.GroupBy(x => x.Metric);
                var filteredPatientMetrics = patientMetricsByMetric.Select(x => x.OrderByDescending(x => x.CreatedAt).First()).ToList();
                
                if (filteredPatientMetrics is null)
                {
                    return patientMetrics;
                }

                return filteredPatientMetrics;
            }

            return patientMetrics;
        }
    }
}
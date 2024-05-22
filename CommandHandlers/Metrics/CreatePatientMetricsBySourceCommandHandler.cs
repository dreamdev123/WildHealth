using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Metrics;
using WildHealth.Application.Services.Metrics;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Metrics;
namespace WildHealth.Application.CommandHandlers.Metrics
{
    public class CreatePatientMetricsBySourceCommandHandler : IRequestHandler<CreatePatientMetricsBySourceCommand, List<PatientMetric>>
    {
        private readonly IDictionary<MetricSource, IList<MetricSource>> _metricSourceDependencies =
            new Dictionary<MetricSource, IList<MetricSource>>()
            {
                {MetricSource.DNA, new List<MetricSource>() {MetricSource.Calculator, MetricSource.AddOnReport}},
                {MetricSource.Labs, new List<MetricSource>() {MetricSource.Calculator}},
            };
        private readonly IMetricService _metricService;
        private readonly IMediator _mediator;

        public CreatePatientMetricsBySourceCommandHandler(
            IMetricService metricService,
            IMediator mediator
        )
        {
            _metricService = metricService;
            _mediator = mediator;
        }

        public async Task<List<PatientMetric>> Handle (CreatePatientMetricsBySourceCommand command, CancellationToken cancellationToken)
        {
            var metricsToCreate = new List<Metric>();

            var sources = ApplyMetricSourceDependencies(command.Sources);
            
            foreach (MetricSource source in sources)
            {
                var sourceMetrics = await _metricService.GetBySource(source);
                metricsToCreate.AddRange(sourceMetrics);
            }

            var patientMetrics = await _mediator.Send(new CreatePatientMetricsCommand(
                patientId: command.PatientId,
                metrics: metricsToCreate.ToArray()
            ));

            return patientMetrics;
        }

        private MetricSource[] ApplyMetricSourceDependencies(MetricSource[] sources)
        {
            return sources.SelectMany(o =>
            {
                var existing = new[] {o};
                return _metricSourceDependencies.ContainsKey(o)
                    ? existing.Concat(_metricSourceDependencies[o]).ToArray()
                    : existing;
            }).Distinct().ToArray();
        }
    }
}
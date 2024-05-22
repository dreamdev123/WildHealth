using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Utils.Metrics;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Metrics;
using WildHealth.Domain.Entities.Reports.Microbiome;
using WildHealth.Report.Calculators;
using WildHealth.Report.Calculators.Microbiome;

namespace WildHealth.Application.CommandHandlers.Metrics.Flows
{
    public class CreateMicrobiomePatientMetricsFlow : IMaterialisableFlow
    {
        private readonly Patient _patient;
        private readonly Metric[] _metrics;
        private readonly InputsAggregator _inputsAggregator;
        private readonly IDictionary<ClassificationType, IList<ClassificationTypeOption>> _classificationTypeOptions;
        private readonly IClassificationTypeOptionHelper _classificationTypeOptionHelper;
        private readonly ICalculator<MicrobiomeResistance> _microbiomeResistanceCalculator = new MicrobiomeResistanceCalculator();
        private readonly ILogger _logger;

        public CreateMicrobiomePatientMetricsFlow(
            Patient patient,
            Metric[] metrics,
            InputsAggregator inputsAggregator,
            IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions,
            IClassificationTypeOptionHelper classificationTypeOptionHelper,
            ILogger logger
        )
        {
            _patient = patient;
            _metrics = metrics.Where(o => o.Source == MetricSource.Microbiome).ToArray();
            _inputsAggregator = inputsAggregator;
            _classificationTypeOptions = classificationTypeOptions;
            _classificationTypeOptionHelper = classificationTypeOptionHelper;
            _logger = logger;
        }
        public MaterialisableFlowResult Execute()
        {   
            var newPatientMetrics = new List<PatientMetric>();
            var microbiomeValues = _microbiomeResistanceCalculator.Calculate(_inputsAggregator).Values;

            foreach (var metric in _metrics)
            {
                var metricData = microbiomeValues.FirstOrDefault(x => x.Name == metric.Identifier);
                
                if (metricData is null || string.IsNullOrEmpty(metricData.Classification))
                {
                    continue;
                }

                var classificationTypeOption = _classificationTypeOptionHelper.GetClassificationTypeOption(_classificationTypeOptions, metric.ClassificationType, metricData.Classification);

                newPatientMetrics.Add(new PatientMetric(
                    patientId: _patient.GetId(),
                    metricId: metric.GetId(),
                    value: metricData.Value.ToString(),
                    valueUnits: MetricConstants.MetricValueTypes.Decimal,
                    classificationTypeOptionId: classificationTypeOption.GetId()
                ));
            }

            if (newPatientMetrics.Any())
            {
                return newPatientMetrics.Select(x => x.Added()).ToFlowResult();
            }

            return MaterialisableFlowResult.Empty;
        }
    }
}
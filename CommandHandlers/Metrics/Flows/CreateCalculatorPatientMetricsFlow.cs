using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Utils.Metrics;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.CommandHandlers.Metrics.Flows
{
    public class CreateCalculatorPatientMetricsFlow : IMaterialisableFlow
    {
        private readonly Patient _patient;
        private readonly Metric[] _metrics;
        private readonly InputsAggregator _inputsAggregator;
        private readonly IDictionary<ClassificationType, IList<ClassificationTypeOption>> _classificationTypeOptions;
        private readonly ICalculatorMetricClassifier _calculatorMetricClassifier;
        private readonly ILogger _logger;

        public CreateCalculatorPatientMetricsFlow(
            Patient patient,
            Metric[] metrics,
            InputsAggregator inputsAggregator,
            IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions,
            ICalculatorMetricClassifier calculatorMetricClassifier,
            ILogger logger
        )
        {
            _patient = patient;
            _metrics = metrics;
            _inputsAggregator = inputsAggregator;
            _classificationTypeOptions = classificationTypeOptions;
            _calculatorMetricClassifier = calculatorMetricClassifier;
            _logger = logger;
        }
        
        public MaterialisableFlowResult Execute()
        {
            var newPatientMetrics = new List<PatientMetric>();
            var patientSnps = new List<string>();
            var genericSnps = new List<IDictionary<string, string>>();

            foreach (var metric in _metrics)
            {
                if (metric is null || metric.Source != MetricSource.Calculator)
                {
                    continue;
                }

                try
                {
                    var newPatientMetric = _calculatorMetricClassifier.GetCalculatorPatientMetric(metric, _inputsAggregator, _classificationTypeOptions);
                    newPatientMetrics.Add(newPatientMetric);
                }
                catch(NullReferenceException nre)
                {
                    _logger.LogError($"Error classifying calculator metric {metric}: {nre.ToString()}");
                }
                catch(Exception e)
                {
                    _logger.LogInformation($"Could not create PatientMetric for {metric.Identifier}. {e}");
                }
                

            }

            if (newPatientMetrics.Any())
            {
                return newPatientMetrics.Select(x => x.Added()).ToFlowResult();
            }

            return MaterialisableFlowResult.Empty;
        }
    }
}
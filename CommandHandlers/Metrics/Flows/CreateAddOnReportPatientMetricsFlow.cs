using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.Reports;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Utils.Metrics;
using WildHealth.Common.Models.Metrics;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.CommandHandlers.Metrics.Flows
{
    public record CreateAddOnReportPatientMetricsFlow : IMaterialisableFlow
    {
        private readonly Patient _patient;
        private readonly Metric[] _metrics;
        private readonly IDictionary<Metric, UnclassifiedPatientMetricModel?> _data;
        private readonly IDictionary<ClassificationType, IList<ClassificationTypeOption>> _classificationTypeOptions;
        private readonly IClassificationTypeOptionHelper _classificationTypeOptionHelper;
        private readonly ILogger _logger;

        public CreateAddOnReportPatientMetricsFlow(
            Patient patient,
            Metric[] metrics,
            IDictionary<Metric, UnclassifiedPatientMetricModel?> data,
            IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions,
            IClassificationTypeOptionHelper classificationTypeOptionHelper,
            ILogger logger
        )
        {
            _patient = patient;
            _metrics = metrics;
            _data = data;
            _classificationTypeOptions = classificationTypeOptions;
            _classificationTypeOptionHelper = classificationTypeOptionHelper;
            _logger = logger;
        }
        
        public MaterialisableFlowResult Execute()
        {
            var newPatientMetrics = new List<PatientMetric>();

            foreach (var metric in _metrics)
            {
                if (metric is null || metric.Source != MetricSource.AddOnReport)
                {
                    continue;
                }

                try
                {
                    var patientMetricData = _data[metric];

                    if (patientMetricData is null)
                    {
                        continue;
                    }

                    var patientMetric = CreatePatientMetricFromData(patientMetricData, _patient.GetId());

                    if (patientMetric is not null)
                    {
                        newPatientMetrics.Add(patientMetric);
                    }
                }
                catch(NullReferenceException nre)
                {
                    _logger.LogError($"Error classifying AddOn Report metric {metric}: {nre.ToString()}");
                }
                catch(Exception e)
                {
                    _logger.LogInformation($"Could not create PatientMetric for {metric.Identifier}. {e}");
                }
            }

            if (newPatientMetrics.Any())
            {
                return newPatientMetrics.Select(x => x.Added()).ToFlowResult()
                    + new PatientAddOnReportMetricsCreatedEvent(_patient.GetId(), newPatientMetrics.ToArray());
            }

            return MaterialisableFlowResult.Empty;
        }

        private PatientMetric? CreatePatientMetricFromData(UnclassifiedPatientMetricModel metricData, int patientId)
        {
            var classificationTypeOption = _classificationTypeOptionHelper.GetClassificationTypeOption(
                _classificationTypeOptions,
                metricData.Metric.ClassificationType,
                metricData.ClassificationString
            );
            
            if (classificationTypeOption is null) return default;

            return new PatientMetric(
                patientId: patientId,
                metricId: metricData.Metric.GetId(),
                value: metricData.Value,
                valueUnits: metricData.ValueType,
                classificationTypeOptionId: classificationTypeOption.GetId()
            );
        }
    }
}
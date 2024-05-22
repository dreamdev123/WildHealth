using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Utils.Metrics;
using WildHealth.Application.Utils.LabNameRangeProvider;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Metrics;
using WildHealth.Domain.Models.Metrics;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Metrics.Flows
{
    public class CreateLabPatientMetricsFlow : IMaterialisableFlow
    {
        private readonly Patient _patient;
        private readonly Metric[] _metrics;
        private readonly InputsAggregator _inputsAggregator;
        private readonly ILabNameRangeProvider _labNameRangeProvider;
        private readonly IWildHealthRangeClassifier _rangeClassifier;
        private readonly IDictionary<ClassificationType, IList<ClassificationTypeOption>> _classificationTypeOptions;
        private readonly IClassificationTypeOptionHelper _classificationTypeOptionHelper;
        private readonly ILogger _logger;

        public CreateLabPatientMetricsFlow(
            Patient patient,
            Metric[] metrics,
            InputsAggregator inputsAggregator,
            IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions,
            ILabNameRangeProvider labNameRangeProvider,
            IWildHealthRangeClassifier rangeClassifier,
            IClassificationTypeOptionHelper classificationTypeOptionHelper,
            ILogger logger
        )
        {
            _patient = patient;
            _metrics = metrics;
            _inputsAggregator = inputsAggregator;
            _labNameRangeProvider = labNameRangeProvider;
            _rangeClassifier = rangeClassifier;
            _classificationTypeOptions = classificationTypeOptions;
            _classificationTypeOptionHelper = classificationTypeOptionHelper;
            _logger = logger;
        }
        public MaterialisableFlowResult Execute()
        {   
            var newPatientMetrics = new List<PatientMetric>();
            foreach (var metric in _metrics)
            {
                if (metric is null || metric.Source != MetricSource.Labs)
                {
                    continue;
                }

                var labInputValue = _inputsAggregator.GetLatestLabInputValue(metric.Identifier);
                
                // GetLatestLabInputValue returns an empty LabInputValue if none found, check for null ID in this case
                if (labInputValue?.Id is null) 
                {
                    continue;
                }

                ClassificationTypeOption classificationOption;

                try
                {
                    classificationOption = GetClassificationTypeOptionForLabInputValue(metric, labInputValue);
                }
                catch (NullReferenceException nre)
                {
                    _logger.LogError($"Error classifying Lab metric {metric}: {nre.ToString()}");
                    continue;
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Error determining ClassificationTypeOption for metric {metric.Identifier} and value {labInputValue.Value}. {e}");
                    continue;
                }


                if (classificationOption is null)
                {
                    _logger.LogInformation($"No ClassificationTypeOption found for metric {metric.Identifier} and value {labInputValue.Value}. LabInputValueId {labInputValue.Id}");
                    continue;
                }

                var valueUnits = labInputValue.GetPriorityRange().Dimension;

                newPatientMetrics.Add( new PatientMetric(
                    patientId: _patient.GetId(),
                    metricId: metric.Id ?? 0,
                    value: labInputValue.Value.ToString(),
                    valueUnits: valueUnits,
                    classificationTypeOptionId: classificationOption.Id ?? 0
                ));
            }

            if (newPatientMetrics.Any())
            {
                return newPatientMetrics.Select(x => x.Added()).ToFlowResult();
            }

            return MaterialisableFlowResult.Empty;
        }

        private ClassificationTypeOption GetClassificationTypeOptionForLabInputValue(Metric metric, LabInputValue labInputValue)
        {
            var classificationTypeDomain = ClassificationTypeDomain.Create(metric.ClassificationType);
            if (classificationTypeDomain.IsWildHealthRange())
            {
                return  _rangeClassifier.GetClassificationTypeOption(_classificationTypeOptions, metric, labInputValue);
            } else
            {
                return GetClassificationForStandardLabValue(metric, labInputValue);
            }
        }

        private ClassificationTypeOption GetClassificationForStandardLabValue(Metric metric, LabInputValue labInputValue)
        {
            var labInputRange = _labNameRangeProvider.GetRange(labInputValue);

            var (isInRange, outOfRangeString) = labInputRange.IsValueInRange(labInputValue.Value);
            
            var classificationType = metric.ClassificationType;

            ClassificationTypeOption classificationOption;

            if (isInRange)
            {
                classificationOption = _classificationTypeOptionHelper.GetClassificationTypeOption(_classificationTypeOptions, classificationType, MetricConstants.ClassificationTypeOptionStrings.Normal);
            } else
            {
                if (outOfRangeString.ToLower().Contains(MetricConstants.ClassificationTypeOptionStrings.Above))
                {
                    classificationOption = _classificationTypeOptionHelper.GetClassificationTypeOption(_classificationTypeOptions, classificationType, MetricConstants.ClassificationTypeOptionStrings.High);
                } else
                {
                    classificationOption = _classificationTypeOptionHelper.GetClassificationTypeOption(_classificationTypeOptions, classificationType, MetricConstants.ClassificationTypeOptionStrings.Low);
                }
            }

            return classificationOption;
        }
    }
}
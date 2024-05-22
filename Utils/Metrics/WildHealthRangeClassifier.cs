using System.Net;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Utils.Metrics
{
    public class WildHealthRangeClassifier : IWildHealthRangeClassifier
    {
        private readonly ILogger<WildHealthRangeClassifier> _logger;
        private readonly IClassificationTypeOptionHelper _classificationTypeOptionHelper;

        public WildHealthRangeClassifier(
            ILogger<WildHealthRangeClassifier> logger,
            IClassificationTypeOptionHelper classificationTypeOptionHelper
        )
        {
            _logger = logger;
            _classificationTypeOptionHelper = classificationTypeOptionHelper;
        }

        public ClassificationTypeOption GetClassificationTypeOption(IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions,Metric metric, LabInputValue labInputValue)
        {
            var value = labInputValue.Value;
            return GetClassificationTypeOption(classificationTypeOptions, metric, value);
        }

        public ClassificationTypeOption GetClassificationTypeOption(IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions,Metric metric, decimal value)
        {

            //Pull from list of metricRanges to get the defined values
            var ranges = GetRangeForMetricIdentifier(metric.Identifier);

            //Iterate over ranges to determine a match
            foreach(var range in ranges)
            {
                var isInRange = range.IsInRange(value);
                if (isInRange)
                {
                    return _classificationTypeOptionHelper.GetClassificationTypeOption(classificationTypeOptions, metric.ClassificationType, range.RangeName);
                }
            }
            
            throw new AppException(HttpStatusCode.NotFound, $"Could not find ClassificationTypeOption range for {metric.Identifier} with value {value}");
        }

        private List<ClassificationRange> GetRangeForMetricIdentifier(string identifier)
        {
            if (!LabMetricRanges.Data.ContainsKey(identifier))
            {
                _logger.LogInformation($"Could not find WildHealthRangeClassifier definition for {identifier}");
                return new List<ClassificationRange>();
            }
            
            return LabMetricRanges.Data[identifier];
        }
    }
}
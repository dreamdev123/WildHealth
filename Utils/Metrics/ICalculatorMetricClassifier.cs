using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Metrics;

namespace WildHealth.Application.Utils.Metrics
{
    public interface ICalculatorMetricClassifier
    {
        public PatientMetric GetCalculatorPatientMetric(Metric metric, InputsAggregator aggregator, IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions);
    }
}
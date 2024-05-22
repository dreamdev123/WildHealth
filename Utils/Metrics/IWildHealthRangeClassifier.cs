using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Metrics;

namespace WildHealth.Application.Utils.Metrics
{
    public interface IWildHealthRangeClassifier
    {
        ClassificationTypeOption GetClassificationTypeOption(IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions, Metric metric, LabInputValue labInputValue);
        ClassificationTypeOption GetClassificationTypeOption(IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions,Metric metric, decimal value);
    }
}
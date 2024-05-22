using System;
using System.Collections.Generic;
using WildHealth.Domain.Entities.Metrics;

namespace WildHealth.Application.Utils.Metrics
{
    public interface IClassificationTypeOptionHelper
    {
        public ClassificationTypeOption GetClassificationTypeOption(
            IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions, 
            ClassificationType classificationType,
            string classificationString
        );
    }
}
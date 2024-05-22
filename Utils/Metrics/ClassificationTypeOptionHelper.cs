using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Utils.Metrics
{
    public class ClassificationTypeOptionHelper : IClassificationTypeOptionHelper
    {
        public ClassificationTypeOption GetClassificationTypeOption(
            IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions,
            ClassificationType classificationType,
            string classificationString
        )
        {
            var options = classificationTypeOptions[classificationType];
            
            if (options is null || !options.Any())
            {
                throw new AppException(HttpStatusCode.NotFound, $"No ClassificationTypeOption found for type {classificationType.Id} and name {classificationString}");
            }

            var classificationTypeOption = options.Where(x => x.Name.ToLower() == classificationString.ToLower()).FirstOrDefault();

            if (classificationTypeOption is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"No ClassificationTypeOption found for type {classificationType.Id} and name {classificationString}");
            }

            return classificationTypeOption;
        }
    }
}
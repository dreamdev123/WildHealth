using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WildHealth.Application.Extensions;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Utils.Reports
{
    public class PatientReportTemplateParser
    {
        private readonly ReportTemplate _reportTemplate;
        private readonly IList<PatientMetric> _patientMetrics;
        private readonly IList<PatientRecommendation> _patientRecommendations;

        public PatientReportTemplateParser(
            ReportTemplate reportTemplate,
            IList<PatientMetric> patientMetrics,
            IList<PatientRecommendation> patientRecommendations
        )
        {
            _reportTemplate = reportTemplate;
            _patientMetrics = patientMetrics;
            _patientRecommendations = patientRecommendations;
        }

        public IDictionary<string, object> GenerateReportContent()
        {
            var patientReportContent = _reportTemplate.Template;
            foreach (var chapter in ((JArray)patientReportContent[ReportConstants.ReportTemplateKeys.Chapters]))
            {
                var pages = chapter[ReportConstants.ReportTemplateKeys.Pages];
                if (pages is null)
                {
                    continue;
                }
                foreach(var page in pages)
                {
                    var sections = page[ReportConstants.ReportTemplateKeys.Sections];
                    if (sections is null)
                    {
                        continue;
                    }
                    foreach(var section in sections)
                    {
                        var sectionType = section[ReportConstants.ReportTemplateKeys.Type];
                        if (sectionType is null || sectionType.ToString() == ReportConstants.ReportTemplateSectionTypes.Generic)
                        {
                            continue;
                        }
                        var sectionContent = section[ReportConstants.ReportTemplateKeys.Content];
                        if (sectionContent is null)
                        {
                            continue;
                        }
                        foreach(JProperty contentItem in sectionContent)
                        {
                            var contentType = ((JProperty)contentItem).Name;
                            switch(contentType)
                            {
                                case ReportConstants.ReportTemplateKeys.ReportValueList:
                                    var reportValueList = contentItem.Value.Children();
                                    foreach(var reportValue in reportValueList)
                                    {
                                        reportValue[ReportConstants.ReportTemplateKeys.Value] = GetValueForReportValue(reportValue);
                                    }
                                    break;
                                case ReportConstants.ReportTemplateKeys.Recommendations:
                                    EnrichRecommendations(contentItem);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            return patientReportContent;
        }

        private void EnrichRecommendations(JProperty contentItem)
        {
            if (contentItem.Value is JObject recommendationObjectItem)
            {
                recommendationObjectItem[ReportConstants.ReportTemplateKeys.Value] = GetRecommendationsContent();   
            }
            else if (contentItem.Value is JArray recommendationArrayItem)
            {
                _patientRecommendations.ForEach(x => recommendationArrayItem.Add(x.Content));
            }
        }

        private string GetValueForReportValue(JToken reportValue)
        {
            var pathToken = reportValue[ReportConstants.ReportTemplateKeys.Key];
            if (reportValue is null || pathToken is null)
            {
                return string.Empty;
            }

            var pathString = pathToken.ToString();

            if (pathString is null) 
            {
                return string.Empty;
            }

            // ReportValue keys are formatted "MetricIdentifier.Field"
            var path = pathString.Split(".");

            var patientMetric = _patientMetrics.Where(x => x.Metric.Identifier == path[0]).FirstOrDefault();

            if (patientMetric is null)
            {
                return string.Empty;
            }

            switch(path[1])
            {
                case ReportConstants.ReportTemplateKeys.Value:
                    return patientMetric.Value;
                case ReportConstants.ReportTemplateKeys.Classification:
                    return patientMetric.ClassificationTypeOption.Name;
                default:
                    return string.Empty;
            }
        }

        private string GetRecommendationsContent()
        {
            return string.Join("\n", _patientRecommendations.Select(x => x.Content));
        }

        private IList<IDictionary<string, object>> ConvertToList(object jarrayObject)
        {
            var obj = ((JArray)jarrayObject).ToObject<IList<IDictionary<string, object>>>();
            if (obj is null)
            {
                throw new AppException(System.Net.HttpStatusCode.BadRequest, "Unable to convert JSON to IList<IDictionary<string, object>>");
            }
            return obj;
        }

        private IDictionary<string, object> ConvertToDictionary(object jsonObject)
        {
            var obj = ((JObject)jsonObject).ToObject<IDictionary<string, object>>();
            if (obj is null)
            {
                throw new AppException(System.Net.HttpStatusCode.BadRequest, "Unable to convert JSON to IDictionary<string, object>");
            }
            return obj;
        }
    }
}
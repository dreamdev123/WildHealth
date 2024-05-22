using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using WildHealth.ClarityCore.Constants;
using WildHealth.ClarityCore.Models.Patients;
using WildHealth.ClarityCore.WebClients.Patients;
using WildHealth.Common.Models.Metrics;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Shared.DistributedCache.Services;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Utils.Metrics;

public class AddOnReportMetricRetriever : IAddOnReportMetricRetriever
{
    private readonly IWildHealthSpecificCacheService<AddOnReportMetricRetriever, AddOnReportModel> _addOnReportModelCache;
    private readonly IPatientsWebClient _patientsWebClient;
    private readonly IDictionary<string, MetricValueRetriever> _metricValueRetrievers;
    private readonly IDictionary<string, string> _metricStringIdentifierDictionary= new Dictionary<string, string>()
    {
        // celiac
        { MetricConstants.CeliacMetricStrings.CeliacDiseaseRisk, AddOnReportConstants.CeliacReportTerms.CeliacDiseaseRisk },
        { MetricConstants.CeliacMetricStrings.GlutenSensitivity, AddOnReportConstants.CeliacReportTerms.GlutenSensitivity },
        { MetricConstants.CeliacMetricStrings.DQ2_2, AddOnReportConstants.CeliacReportTerms.DQ2_2 },
        { MetricConstants.CeliacMetricStrings.DQ2_5, AddOnReportConstants.CeliacReportTerms.DQ2_5 },
        { MetricConstants.CeliacMetricStrings.DQ8, AddOnReportConstants.CeliacReportTerms.DQ8 },
        // sleep
        { MetricConstants.SleepMetricStrings.SleepChronotype, AddOnReportConstants.SleepReportTerms.Chronotype },
        { MetricConstants.SleepMetricStrings.BedTime, AddOnReportConstants.SleepReportTerms.BedTime },
        { MetricConstants.SleepMetricStrings.Asleep, AddOnReportConstants.SleepReportTerms.Asleep },
        { MetricConstants.SleepMetricStrings.WakeUpTime, AddOnReportConstants.SleepReportTerms.WakeUpTime },
        { MetricConstants.SleepMetricStrings.OptimalSleepDurationInHours, AddOnReportConstants.SleepReportTerms.OptimalSleepDurationInHours },
        { MetricConstants.SleepMetricStrings.EaseOfWalking, AddOnReportConstants.SleepReportTerms.EaseOfWalking },
        { MetricConstants.SleepMetricStrings.SleepQuality, AddOnReportConstants.SleepReportTerms.SleepQuality },
        { MetricConstants.SleepMetricStrings.Snoring, AddOnReportConstants.SleepReportTerms.Snoring },
        { MetricConstants.SleepMetricStrings.SleepDisruption, AddOnReportConstants.SleepReportTerms.SleepDisruption },
        { MetricConstants.SleepMetricStrings.Insomnia, AddOnReportConstants.SleepReportTerms.Insomnia },
        { MetricConstants.SleepMetricStrings.CaffeineInducedInsomnia, AddOnReportConstants.SleepReportTerms.CaffeineInducedInsomnia },
        { MetricConstants.SleepMetricStrings.DaytimeSleepiness, AddOnReportConstants.SleepReportTerms.DaytimeSleepiness },
        { MetricConstants.SleepMetricStrings.SleepLatency, AddOnReportConstants.SleepReportTerms.SleepLatency },
        { MetricConstants.SleepMetricStrings.Bruxism, AddOnReportConstants.SleepReportTerms.Bruxism },
        { MetricConstants.SleepMetricStrings.RestlessLegsSyndrome, AddOnReportConstants.SleepReportTerms.RestlessLegsSyndrome },
        { MetricConstants.SleepMetricStrings.SnoringOsa, AddOnReportConstants.SleepReportTerms.SnoringOsa }
    };

    public AddOnReportMetricRetriever(
        IWildHealthSpecificCacheService<AddOnReportMetricRetriever, AddOnReportModel> addOnReportModelCache,
        IPatientsWebClient patientsWebClient
    )
    {
        _addOnReportModelCache = addOnReportModelCache;
        _patientsWebClient = patientsWebClient;
        _metricValueRetrievers =
            new Dictionary<string, MetricValueRetriever>()
            {
                // celiac
                { MetricConstants.CeliacMetricStrings.DQ2_2, async (metric, patientId) => await GetCeliacPatientMetric(metric, patientId) },
                { MetricConstants.CeliacMetricStrings.DQ2_5, async (metric, patientId) => await GetCeliacPatientMetric(metric, patientId) },
                { MetricConstants.CeliacMetricStrings.DQ8, async (metric, patientId) => await GetCeliacPatientMetric(metric, patientId) },
                { MetricConstants.CeliacMetricStrings.GlutenSensitivity, async (metric, patientId) => await GetCeliacScorePatientMetric(metric, patientId) },
                { MetricConstants.CeliacMetricStrings.CeliacDiseaseRisk, async (metric, patientId) => await GetCeliacScorePatientMetric(metric, patientId) },
                // sleep
                { MetricConstants.SleepMetricStrings.SleepChronotype, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.BedTime, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.Asleep, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.WakeUpTime, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.OptimalSleepDurationInHours, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.EaseOfWalking, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.SleepQuality, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.Snoring, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.SleepDisruption, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.Insomnia, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.CaffeineInducedInsomnia, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.DaytimeSleepiness, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.SleepLatency, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.Bruxism, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.RestlessLegsSyndrome, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) },
                { MetricConstants.SleepMetricStrings.SnoringOsa, async (metric, patientId) => await GetSleepScorePatientMetric(metric, patientId) }
            };
    }

    public async Task<UnclassifiedPatientMetricModel?> Get(Metric metric, int patientId)
    {
        if (!_metricValueRetrievers.ContainsKey(metric.Identifier))
        {
            return null;
        }

        return await _metricValueRetrievers[metric.Identifier](metric, patientId);
    }
    
    
    private AddOnReportModel _celiacReportData = new AddOnReportModel();
    
    /// <summary>
    /// Since this is cached based on patientId, we need to pass in that parameter and construct a key with that as a dependency
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    private async Task<AddOnReportModel> GetCeliacReportData(int patientId)
    {
        var key = $"GetCeliacReportData-{patientId}";
        
        return await _addOnReportModelCache.GetAsync(
                key: key,
                getter: async () => await _patientsWebClient.GetPatientAddOnReport(patientId, ReportConstants.ReportTypeStrings.Celiac)
            );
    }

    
    private async Task<AddOnReportModel> GetSleepReportData(int patientId)
    {
        var key = $"GetSleepReportData-{patientId}";
        return await _addOnReportModelCache.GetAsync(
            key: key,
            getter: async () => await _patientsWebClient.GetPatientAddOnReport(patientId, ReportConstants.ReportTypeStrings.Sleep)
        );
    }
    
    private bool AssertClassificationValue(string? classification)
    {
        return classification is not null && classification != "null";
    }
    private async Task<bool> AssertValidCeliacReport(int patientId)
    {
        var reportData = await GetCeliacReportData(patientId);
        return reportData.ScoreClassifications[AddOnReportConstants.CeliacReportTerms.CeliacDiseaseRisk] is not null;
    }

    private async Task<UnclassifiedPatientMetricModel?> GetCeliacPatientMetric(Metric metric, int patientId)
    {
        var reportData = await GetCeliacReportData(patientId);
        var classificationIdentifier = GetClassificationIdentifier(metric);
        var classification = reportData.ScoreClassifications[classificationIdentifier];
        if (!AssertClassificationValue(classification) || !(await AssertValidCeliacReport(patientId))) return default;
        return CreateUnclassifiedPatientMetric(patientId, metric, classification, MetricConstants.MetricValueTypes.Bool, classification);
    }

    private async Task<UnclassifiedPatientMetricModel?> GetCeliacScorePatientMetric(Metric metric, int patientId)
    {
        var reportData = await GetCeliacReportData(patientId);
        var classificationIdentifier = GetClassificationIdentifier(metric);
        var classification = reportData.ScoreClassifications[classificationIdentifier];
        var score = reportData.ScoreValues[classificationIdentifier];
        if (score is null) return default;
        return CreateUnclassifiedPatientMetric(patientId, metric, ((decimal)score).ToString(), MetricConstants.MetricValueTypes.Decimal, classification);
    }
    
    private async Task<UnclassifiedPatientMetricModel?> GetSleepScorePatientMetric(Metric metric, int patientId)
    {
        var reportData = await GetSleepReportData(patientId);
        var classificationIdentifier = GetClassificationIdentifier(metric);
        if (!reportData.ScoreClassifications.ContainsKey(classificationIdentifier)) return default;
        var classification = reportData.ScoreClassifications[classificationIdentifier];
        var score = reportData.ScoreValues[classificationIdentifier];
        if (score is null) return default;
        return CreateUnclassifiedPatientMetric(patientId, metric, ((decimal)score).ToString(), MetricConstants.MetricValueTypes.Decimal, classification);
    }

    private string GetClassificationIdentifier(Metric metric)
    {
        return _metricStringIdentifierDictionary.ContainsKey(metric.Identifier)
            ? _metricStringIdentifierDictionary[metric.Identifier]
            : throw new AppException(HttpStatusCode.NotFound,
                $"Unable to locate clarity core equivalent identifier for metric identifier: {metric.Identifier}");
    }
    
    private UnclassifiedPatientMetricModel CreateUnclassifiedPatientMetric(int patientId, Metric metric, string valueString, string valueUnits, string classificationString)
    {
        return new UnclassifiedPatientMetricModel() {
            PatientId = patientId,
            Metric = metric,
            Value = valueString,
            ValueType = valueUnits,
            ClassificationString = classificationString
        }; 
    }
}
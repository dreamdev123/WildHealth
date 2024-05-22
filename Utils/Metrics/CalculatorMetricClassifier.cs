using System.Collections.Generic;
using WildHealth.ClarityCore.WebClients.Patients;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Report.Calculators.Alpha;
using WildHealth.Report.Calculators.DietAndNutrition;
using WildHealth.Report.Calculators.ExerciseAndRecovery;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Utils.Metrics
{
    public class CalculatorMetricClassifier : ICalculatorMetricClassifier
    {
        private readonly IPatientsWebClient _patientsWebClient;
        private readonly IClassificationTypeOptionHelper _classificationTypeOptionHelper;
        private readonly IWildHealthRangeClassifier _wildHealthRangeClassifier;
        public CalculatorMetricClassifier(
            IPatientsWebClient patientsWebClient,
            IClassificationTypeOptionHelper classificationTypeOptionHelper,
            IWildHealthRangeClassifier wildHealthRangeClassifier)
        {
            _patientsWebClient = patientsWebClient;
            _classificationTypeOptionHelper = classificationTypeOptionHelper;
            _wildHealthRangeClassifier = wildHealthRangeClassifier;
        }
        public PatientMetric GetCalculatorPatientMetric(Metric metric, InputsAggregator aggregator, IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions)
        {
            if (metric is null)
            {
                throw new AppException(System.Net.HttpStatusCode.BadRequest);
            }
            var identifier = metric.Identifier;

            switch(identifier)
            {
                case MetricConstants.CalculatorMetricStrings.Apoe:
                    var apoeCalculator = new ApoeAccuracyCalculator(_patientsWebClient);
                    var apoeResult = apoeCalculator.Calculate(aggregator);
                    return CreatePatientMetricForResult(aggregator.PatientId, metric, apoeResult.Apoe, apoeResult.Apoe, classificationTypeOptions);
                case MetricConstants.CalculatorMetricStrings.CarbRisk:
                    var carbRiskCalculator = new CarbonIntoleranceScoreCalculator();
                    var carbRiskResult = carbRiskCalculator.Calculate(aggregator);
                    var carbRiskClassifierString = ModifiedRiskDetailedToClassificationMapping[carbRiskResult.Result];
                    return CreatePatientMetricForResult(aggregator.PatientId, metric, carbRiskResult.ScorePercent.ToString(), carbRiskClassifierString, classificationTypeOptions);
                case MetricConstants.CalculatorMetricStrings.CholineRiskScore:
                    var cholineRiskCalculator = new CholineRiskCalculator();
                    var cholineRiskResult = cholineRiskCalculator.Calculate(aggregator);
                    var cholineRiskClassifierString = ModifiedRiskDetailedToClassificationMapping[cholineRiskResult.Result];
                    return CreatePatientMetricForResult(aggregator.PatientId, metric, cholineRiskResult.ScorePercent.ToString(), cholineRiskClassifierString, classificationTypeOptions);
                case MetricConstants.CalculatorMetricStrings.FatRisk:
                    var fatRiskCalculator = new FatIntoleranceScoreCalculator();
                    var fatRiskResult = fatRiskCalculator.Calculate(aggregator);
                    var fatRiskClassifierString = ModifiedRiskDetailedToClassificationMapping[fatRiskResult.Result];
                    return CreatePatientMetricForResult(aggregator.PatientId, metric, fatRiskResult.ScorePercent.ToString(), fatRiskClassifierString, classificationTypeOptions);
                case MetricConstants.CalculatorMetricStrings.FolateRiskScore:
                    var folateRiskCalculator = new FolateRiskCalculator();
                    var folateRiskResult = folateRiskCalculator.Calculate(aggregator);
                    var folateRiskClassifierString = RiskDetailedToClassificationMapping[folateRiskResult.Result];
                    return CreatePatientMetricForResult(aggregator.PatientId, metric, folateRiskResult.ScorePercent.ToString(), folateRiskClassifierString, classificationTypeOptions);
                case MetricConstants.CalculatorMetricStrings.InsulinResistance:
                    var insulinResistanceCalculator = new InsulinResistanceScoreCalculator();
                    var insulinResistanceResult = insulinResistanceCalculator.Calculate(aggregator);
                    var insulinResistanceClassifierString = RiskToClassificationMapping[insulinResistanceResult.Result];
                    return CreatePatientMetricForResult(aggregator.PatientId, metric, insulinResistanceResult.ScorePercent.ToString(), insulinResistanceClassifierString, classificationTypeOptions);  
                case MetricConstants.CalculatorMetricStrings.Recovery:
                    var recoveryCalculator = new RecoveryScoreCalculator();
                    var recoveryResult = recoveryCalculator.Calculate(aggregator);
                    return CreatePatientMetricForResult(aggregator.PatientId, metric, recoveryResult.ScorePercent.ToString(), recoveryResult.Result.ToString(), classificationTypeOptions);  
                case MetricConstants.CalculatorMetricStrings.SatFatRisk:
                    var satFatRiskCalculator = new SatFatIntoleranceScoreCalculator();
                    var satFatRiskResult = satFatRiskCalculator.Calculate(aggregator);
                    var satFatRiskClassifierString = ModifiedRiskDetailedToClassificationMapping[satFatRiskResult.Result];
                    return CreatePatientMetricForResult(aggregator.PatientId, metric, satFatRiskResult.ScorePercent.ToString(), satFatRiskClassifierString, classificationTypeOptions); 
                case MetricConstants.CalculatorMetricStrings.StrengthAndEndurance:
                    var strengthAndEnduranceCalculator = new StrengthAndEnduranceScoreCalculator();
                    var strengthAndEnduranceResult = strengthAndEnduranceCalculator.Calculate(aggregator);
                    return CreatePatientMetricForResult(aggregator.PatientId, metric, strengthAndEnduranceResult.ScorePercent.ToString(), strengthAndEnduranceResult.Result.ToString(), classificationTypeOptions); 
                // HdlTrigRatio and Homir are special cases where the calculator returns a decimal, not a risk type
                // use the WildHealthRangeClassifier to get a ClassificationTypeOption for the value
                case MetricConstants.CalculatorMetricStrings.HdlTrigRatio:
                    var hdltrigRatioCalculator = new HdlTrigRatioCalculator();
                    var hdltrigRatioResult = hdltrigRatioCalculator.Calculate(aggregator);
                    var hdltrigRatioClassificationType = _wildHealthRangeClassifier.GetClassificationTypeOption(classificationTypeOptions, metric, hdltrigRatioResult);
                    return CreatePatientMetricForClassificationType(aggregator.PatientId, metric, hdltrigRatioResult.ToString(), MetricConstants.MetricValueTypes.Decimal, hdltrigRatioClassificationType.GetId());
                case MetricConstants.CalculatorMetricStrings.Homir:
                    var homirCalculator = new HomirCalculator();
                    var homirResult = homirCalculator.Calculate(aggregator);
                    var homirClassificationType = _wildHealthRangeClassifier.GetClassificationTypeOption(classificationTypeOptions, metric, homirResult);
                    return CreatePatientMetricForClassificationType(aggregator.PatientId, metric, homirResult.ToString(), MetricConstants.MetricValueTypes.Decimal, homirClassificationType.GetId());
                case MetricConstants.CalculatorMetricStrings.RecommendedMacros:
                    var recommendedMacroCalculator = new DietRecommendedMacrosCalculator();
                    var recommendedMacroResult = recommendedMacroCalculator.Calculate(aggregator);
                    var recommendedMacroString =
                        $"{recommendedMacroResult.Carb},{recommendedMacroResult.Fat},{recommendedMacroResult.Pro}";
                    var recommendedMacroClassificationType = _classificationTypeOptionHelper.GetClassificationTypeOption(classificationTypeOptions, metric.ClassificationType, "True");
                    return CreatePatientMetricForClassificationType(aggregator.PatientId, metric, recommendedMacroString, MetricConstants.MetricValueTypes.String,  recommendedMacroClassificationType.GetId());
                default:
                    throw new AppException(System.Net.HttpStatusCode.NotImplemented, $"Calculator Classifier not implemented for {metric.Identifier}");
            }
        }

        private PatientMetric CreatePatientMetricForResult(int patientId, Metric metric, string valueString, string classifierString, IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions)
        {
            var classificationTypeOption = TryGetClassificationTypeId(metric, classifierString, classificationTypeOptions);
            return CreatePatientMetricForClassificationType(patientId, metric, valueString, MetricConstants.MetricValueTypes.Decimal, classificationTypeOption);
        }

        private PatientMetric CreatePatientMetricForClassificationType(int patientId, Metric metric, string valueString, string valueUnits, int classificationTypeOptionId)
        {
            return new PatientMetric(
                patientId: patientId,
                metricId: metric.Id ?? 0,
                value: valueString,
                valueUnits: valueUnits,
                classificationTypeOptionId: classificationTypeOptionId
            ); 
        }

        private int TryGetClassificationTypeId(Metric metric, string classificationString, IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions)
        {
            try
            {
                if (string.IsNullOrEmpty(classificationString)) 
                {
                    throw new AppException(System.Net.HttpStatusCode.NotFound, $"No classification found for {metric.Identifier} {classificationString}");
                }
                var classification = _classificationTypeOptionHelper.GetClassificationTypeOption(classificationTypeOptions, metric.ClassificationType, classificationString);
                return classification.Id ?? throw new AppException(System.Net.HttpStatusCode.BadGateway, $"No classification found for {metric.Identifier} {classificationString}");
            }
            catch
            {
                throw new AppException(System.Net.HttpStatusCode.NotFound, $"No classification found for {metric.Identifier} {classificationString}");
            }
        }

        private static readonly IDictionary<Risk, string> RiskToClassificationMapping = new Dictionary<Risk, string>() {
            {Risk.Optimal, MetricConstants.ClassificationTypeOptionStrings.Low},
            {Risk.Moderate, MetricConstants.ClassificationTypeOptionStrings.Medium},
            {Risk.High, MetricConstants.ClassificationTypeOptionStrings.High}
        };

        private static readonly IDictionary<RiskDetailed, string> RiskDetailedToClassificationMapping = new Dictionary<RiskDetailed, string>() {
            {RiskDetailed.Optimal, MetricConstants.ClassificationTypeOptionStrings.Low},
            {RiskDetailed.Normal, MetricConstants.ClassificationTypeOptionStrings.MediumLow},
            {RiskDetailed.Moderate, MetricConstants.ClassificationTypeOptionStrings.Medium},
            {RiskDetailed.Advanced, MetricConstants.ClassificationTypeOptionStrings.MediumHigh},
            {RiskDetailed.High, MetricConstants.ClassificationTypeOptionStrings.High}
        };

        private static readonly IDictionary<RiskDetailed, string> ModifiedRiskDetailedToClassificationMapping = new Dictionary<RiskDetailed, string>() {
            {RiskDetailed.Optimal, MetricConstants.ClassificationTypeOptionStrings.Low},
            {RiskDetailed.Normal, MetricConstants.ClassificationTypeOptionStrings.Low},
            {RiskDetailed.Moderate, MetricConstants.ClassificationTypeOptionStrings.High},
            {RiskDetailed.Advanced, MetricConstants.ClassificationTypeOptionStrings.High},
            {RiskDetailed.High, MetricConstants.ClassificationTypeOptionStrings.High}
        };
    }
}
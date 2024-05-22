using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Recommendations;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.Spreadsheets;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.DistributedCache.Services;
using WildHealth.Shared.Exceptions;
using LogicalOperator = WildHealth.Shared.Enums.LogicalOperator;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class UploadRecommendationFileCommandHandler : IRequestHandler<UploadRecommendationFileCommand>
    {
        private const string Content = "content";
        private const string Ignore = "ignore";
        private const string Type = "recommendationtype";
        private const string Measure = "measuremetric";
        private const string FormulaId = "formulaid";
        private const string FormId = "formid";
        private const string CategoryValue = "Category Value";
        private const string CompareValue = "comparevalue";
        private const string MeasureOperator = "measureoperator";
        private const string LogicalOperator = "logicaloperator";
        private const string RecommendationType = "recommendationtype";
        private const string Classification = "classification";
        // private const string Tag1 = "Tag 1";
        // private const string Tag2 = "Tag 2";
        // private const string Tag3 = "Tag 3";
        // private const string Tag4 = "Tag 4";
        // private const string Tag5 = "Tag 5";
        // private const string Tag6 = "Tag 6";
        private IDictionary<string, RecommendationTrigger> _triggers;

        private readonly ILogger<UploadRecommendationFileCommandHandler> _logger;
        private readonly IGeneralRepository<Recommendation> _recommendationsRepository;
        private readonly IGeneralRepository<Metric> _metricsRepository;
        private readonly IGeneralRepository<ClassificationTypeOption> _classificationTypeOptionsRepository;

        public UploadRecommendationFileCommandHandler(
            ILogger<UploadRecommendationFileCommandHandler> logger,
            IGeneralRepository<Recommendation> recommendationsRepository,
            IGeneralRepository<Metric> metricsRepository,
            IGeneralRepository<ClassificationTypeOption> classificationTypeOptionsRepository
            )
        {
            _logger = logger;
            _recommendationsRepository = recommendationsRepository;
            _metricsRepository = metricsRepository;
            _classificationTypeOptionsRepository = classificationTypeOptionsRepository;
            _triggers = new Dictionary<string, RecommendationTrigger>();
        }

        public async Task Handle(UploadRecommendationFileCommand command, CancellationToken cancellationToken)
        {
            var spreadsheetIterator = new SpreadsheetIterator(command.File, command.StartAtRow);
            
            var importantTitles = new Dictionary<string, string>
            {
                { Content, string.Empty },
                { Ignore, string.Empty },
                { FormulaId, string.Empty },
                { FormId, string.Empty },
                { Type, string.Empty },
                { Measure, string.Empty },
                // { CategoryValue, string.Empty },
                { CompareValue, string.Empty },
                { MeasureOperator, string.Empty },
                { LogicalOperator, string.Empty },
                { Classification, string.Empty },
                // { Tag1, string.Empty },
                // { Tag2, string.Empty },
                // { Tag3, string.Empty },
                // { Tag4, string.Empty },
                // { Tag5, string.Empty },
                // { Tag6, string.Empty },
            };

            try
            {
                await spreadsheetIterator.Iterate(importantTitles, async (rowResults) =>
                {
                    var content = rowResults[Content];
                    var ignore = rowResults[Ignore];
                    var formulaId = rowResults[FormulaId];
                    var formId = rowResults[FormId];
                    var type = rowResults[Type];
                    var metricName = rowResults[Measure];
                    var classificationTypeOptionName = rowResults[Classification];
                    // var compareValue = rowResults[CompareValue];
                    var measureOperator = rowResults[MeasureOperator];
                    var logicalOperator = rowResults[LogicalOperator];
                    var recommendationType = rowResults[RecommendationType];
                    
                    // var tag1 = rowResults[Tag1];
                    // var tag2 = rowResults[Tag2];
                    // var tag3 = rowResults[Tag3];
                    // var tag4 = rowResults[Tag4];
                    // var tag5 = rowResults[Tag5];
                    // var tag6 = rowResults[Tag6];

                    formId = formId.Replace("A", "");

                    _logger.LogInformation($"Adding recommendation: {content}");

                    if (!string.IsNullOrEmpty(ignore) && Convert.ToBoolean(ignore))
                    {
                        return;
                    }

                    _logger.LogInformation($"NOT ignoring");

                    var recommendation = await GetOrCreateRecommendationFromContent(content);
                    
                    _logger.LogInformation($"Have [RecommendationId]: {recommendation.GetId()}");

                    // var tags = ConvertStringsToRecommendationCategoryTag(new[]
                    // {
                    //     type, tag1, tag2, tag3, tag4, tag5, tag6
                    // });
                    //
                    // AddTagsToRecommendation(recommendation, tags);
                    
                    _logger.LogInformation($"Added tags to [RecommendationId] = {recommendation.GetId()}");
                    
                    _logger.LogInformation($"Adding trigger for [classificationTypeOptionName] = {classificationTypeOptionName}");

                    await AddTriggerAndComponent(recommendation, metricName, classificationTypeOptionName, logicalOperator, recommendationType, measureOperator, formId);

                     _recommendationsRepository.Edit(recommendation);

                    await _recommendationsRepository.SaveAsync();

                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Failed to change health coaches supplied patients, {ex}");

                throw;
            }
        }

        private WildHealth.Shared.Enums.LogicalOperator GetLogicalOperatorFromString(string value)
        {
            switch (value)
            {
                case "And":
                case "and":
                    return WildHealth.Shared.Enums.LogicalOperator.And;
                case "Or":
                case "or":
                    return WildHealth.Shared.Enums.LogicalOperator.Or;
                default:
                    throw new AppException(HttpStatusCode.BadRequest, $"Unable to determine logical operator: {value}");
            }
        }

        private string GetProperClassificationTypeOption(string value)
        {
            switch (value)
            {
                case "Homogeneous Effect":
                    return "Homogeneous Variant";
                case "Heterogeneous Effect":
                    return "Heterogeneous Variant";
                case "Homogeneous Wild":
                    return "Homogeneous Wild";
                case "High":
                case "high":
                    return "High";
                case "Medium":
                    return "Medium";
                case "Low":
                    return "Low";
                case "Apo-ε3/ε4":
                    return "Apo-ε3/ε4";
                case "Apo-ε4/ε4":
                    return "Apo-ε4/ε4";
                case "Present":
                    return "Present";
                case "Absent":
                    return "Absent";
                default:
                    return value;
            }
        }

        private async Task AddTriggerAndComponent(
            Recommendation recommendation, 
            string metricName,
            string classificationTypeOptionName, 
            string logicalOperatorString,
            string recommendationTypeString,
            string measureOperatorString,
            string formulaId)
        {
            var logicalOperator = GetLogicalOperatorFromString(logicalOperatorString);
            // var measureOperator = GetOperatorFromString(measureOperatorString);
            var formulaIdInt = Convert.ToInt16(formulaId);

            var recommendationType = Enum.Parse<RecommendationType>(recommendationTypeString);

            var classificationTypeOptionNameProper = GetProperClassificationTypeOption(classificationTypeOptionName);

            var metric = await _metricsRepository
                .All()
                .Where(o => o.Label == metricName)
                .Include(o => o.ClassificationType).ThenInclude(o => o.Options)
                .FirstOrDefaultAsync();

            if (metric is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, $"provided improper metric name: {metricName}");
            }
            
            var classificationTypeOption = metric.ClassificationType.Options
                .FirstOrDefault(o => o.Name == classificationTypeOptionNameProper);

            var trigger = recommendation.RecommendationTriggers
                .FirstOrDefault(o => o.LegacyFormulaId == formulaIdInt);

            if (trigger is null)
            {
                trigger = new RecommendationTrigger()
                {
                    LogicalOperator = logicalOperator,
                    Type = recommendationType,
                    RecommendationTriggerComponents = new List<RecommendationTriggerComponent>(),
                    LegacyFormulaId = formulaIdInt
                };
            
                recommendation.RecommendationTriggers.Add(trigger);
            }

            var component = trigger.RecommendationTriggerComponents.FirstOrDefault(o =>
                o.Metric == metric && o.ClassificationTypeOption == classificationTypeOption);

            if (component is null)
            {
                trigger.RecommendationTriggerComponents.Add(
                    new ()
                    {
                        Metric = metric,
                        ClassificationTypeOption = classificationTypeOption
                    });
            }
        }

        private Operator GetOperatorFromString(string measureOperator)
        {
            return measureOperator switch
            {
                "Eq" => Operator.Eq,
                "Ne" => Operator.Ne,
                "Gt" => Operator.Gt,
                "Lt" => Operator.Lt,
                "Ge" => Operator.Ge,
                "Le" => Operator.Le,
                _ => throw new AppException(HttpStatusCode.BadRequest, $"Unable to parse the measure operator")
            };
        }
        
        
        //
        // [Description("!=")]
        // Ne =2,
        //
        // [Description(">")]
        // Gt = 3,
        //
        // [Description("<")]
        // Lt = 4,
        //
        // [Description(">=")]
        // Ge = 5,
        //
        // [Description("<=")]
        // Le = 6

        private async Task<Recommendation> GetOrCreateRecommendationFromContent(string content)
        {
            var recommendation = await _recommendationsRepository
                .All()
                .Include(o => o.Tags)
                .Include(o => o.RecommendationTriggers).ThenInclude(o => o.RecommendationTriggerComponents).ThenInclude(o => o.Metric)
                .Where(o => o.Content == content)
                .FirstOrDefaultAsync();

            if (recommendation is not null)
            {
                return recommendation;
            }

            recommendation = new Recommendation()
            {
                Content = content,
                Tags = new List<RecommendationTag>(),
                RecommendationTriggers = new List<RecommendationTrigger>()
            };

            await _recommendationsRepository.AddAsync(recommendation);

            await _recommendationsRepository.SaveAsync();

            return recommendation;
        }

        private void AddTagsToRecommendation(Recommendation recommendation, HealthCategoryTag[] tags)
        {
            foreach (var tag in tags)
            {
                if (recommendation.Tags.All(o => o.Tag != tag))
                {
                    recommendation.Tags.Add(new RecommendationTag()
                    {
                        Tag = tag
                    });
                }
            }
        }

        private HealthCategoryTag[] ConvertStringsToRecommendationCategoryTag(string[] tags)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return tags.Select(o => GetRecommendationCategoryTagForString(o))
                .Where(o => o != null)
                .Select(o => o ?? HealthCategoryTag.Unknown)
                .ToArray();
        }

        private HealthCategoryTag? GetRecommendationCategoryTagForString(string value)
        {
            switch (value)
            {
                case "Sleep":
                    return HealthCategoryTag.Sleep;
                case "Neurobehavioral":
                case "Neuro":
                    return HealthCategoryTag.Neurobehavioral;
                case "Macronutrient":
                    return HealthCategoryTag.MacroNutrient;
                case "VitaminsAndMicronutrients":
                    return HealthCategoryTag.VitaminsAndMicronutrients;
                case "KryptoniteFoods":
                    return HealthCategoryTag.KryptoniteFoods;
                case "SuperFoods":
                    return HealthCategoryTag.SuperFoods;
                case "Microbiome":
                    return HealthCategoryTag.Microbiome;
                case "ExerciseAndRecovery":
                case "Exercise/Recovery":
                    return HealthCategoryTag.ExerciseAndRecovery;
                case "Longevity":
                    return HealthCategoryTag.Longevity;
                case "Cardiovascular":
                    return HealthCategoryTag.Cardiovascular;
                case "Metabolic":
                    return HealthCategoryTag.Metabolic;
                case "Inflammation":
                    return HealthCategoryTag.Inflammation;
                case "Dementia":
                    return HealthCategoryTag.Dementia;
                case "Hormones":
                    return HealthCategoryTag.Hormones;
                case "CVD":
                    return HealthCategoryTag.Cvd;
                case "Cancer":
                    return HealthCategoryTag.Cancer;
                case "Autoimmune":
                    return HealthCategoryTag.Autoimmune;
                case "Nutrition":
                    return HealthCategoryTag.Nutrition;
                case "PGX":
                case "PGx":
                    return HealthCategoryTag.Pgx;
                case "Mindfulness":
                    return HealthCategoryTag.Mindfulness;
                case "Methylation":
                    return HealthCategoryTag.Methylation;
                case "RecommendedMacros":
                    return HealthCategoryTag.RecommendedMacros;
                default:
                    return null;
            }
        }
    }
}
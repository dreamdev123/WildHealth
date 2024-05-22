using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.HealthReports;
using WildHealth.Application.Extensions.HealthReports;
using WildHealth.Report.Services.RecommendationsGenerator;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Application.Services.HealthReports;
using WildHealth.Application.Services.Inputs;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Application.Events.Reports;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;

namespace WildHealth.Application.CommandHandlers.HealthReports;

public class GenerateNewHealthReportCommandHandler : IRequestHandler<GenerateNewHealthReportCommand, HealthReport>
{
    private readonly IRecommendationsGenerator _recommendationsGenerator;
    private readonly IHealthReportService _healthReportService;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly ITransactionManager _transactionManager;
    private readonly IInputsService _inputsService;
    private readonly IMediator _mediator;

    public GenerateNewHealthReportCommandHandler(
        IRecommendationsGenerator recommendationsGenerator,
        IHealthReportService healthReportService,
        IFeatureFlagsService featureFlagsService,
        ITransactionManager transactionManager,
        IInputsService inputsService, 
        IMediator mediator)
    {
        _recommendationsGenerator = recommendationsGenerator;
        _healthReportService = healthReportService;
        _featureFlagsService = featureFlagsService;
        _transactionManager = transactionManager;
        _inputsService = inputsService;
        _mediator = mediator;
    }
        
    public async Task<HealthReport> Handle(GenerateNewHealthReportCommand command, CancellationToken cancellationToken)
    {
        var aggregator = await _inputsService.GetAggregatorAsync(command.PatientId, true);

        RequiredDataValidation(aggregator);
            
        var latestReport = await FetchLatestHealthReport(command.PatientId);

        // If current report has Preparing status - use regeneration flow
        if (latestReport != null && latestReport.Status.Status == HealthReportStatus.Preparing)
        {
            var regenerateCommand = new RegenerateLatestHealthReportCommand(
                patientId: command.PatientId, 
                recommendations: command.Recommendations
            );
            
            return await _mediator.Send(regenerateCommand, cancellationToken);
        }

        await using var transaction = _transactionManager.BeginTransaction();
        
        try
        {
            if (latestReport != null)
            {
                if (!latestReport.IsSubmitted())
                {
                    throw new AppException(HttpStatusCode.BadRequest, "The current report must be published before generating a new report");
                }
            }
            
          
            var report = await _healthReportService.CreateAsync(command.PatientId);
            
            if (latestReport != null && command.PrefillReport)
            {
                report = await PrefillAsync(report, latestReport);
            }
            
            await _healthReportService.GenerateAsync(report, aggregator);
            
            if (_featureFlagsService.GetFeatureFlag(FeatureFlags.RecommendationsV2))
            {
                await _recommendationsGenerator.SetRecommendationsV2Async(
                    recommendations: command.Recommendations,
                    healthReport: report
                );
            }
            else
            {
                await _recommendationsGenerator.SetRecommendationsAsync(
                    recommendations: command.Recommendations,
                    healthReport: report,
                    aggregator: aggregator
                );
            }

            // update - calculate Apoe information for HealthSummary
            var reportRegenerateEvent = new HealthReportUpdatedEvent(command.PatientId, report);
            await _mediator.Publish(reportRegenerateEvent, cancellationToken);
            
            await transaction.CommitAsync(cancellationToken);

            return report;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
        
    #region private

    /// <summary>
    /// Prefills report based on draft report
    /// </summary>
    /// <param name="report"></param>
    /// <param name="draft"></param>
    /// <returns></returns>
    private async Task<HealthReport> PrefillAsync(HealthReport report, HealthReport draft)
    {
        report.DietAndNutritionReport.MacronutrientRecommendation.MapRecommendations(draft.DietAndNutritionReport.MacronutrientRecommendation);
        report.DietAndNutritionReport.MethylationRecommendation.MapRecommendations(draft.DietAndNutritionReport.MethylationRecommendation);
        report.DietAndNutritionReport.CompleteDietRecommendation.MapRecommendations(draft.DietAndNutritionReport.CompleteDietRecommendation);
        report.DietAndNutritionReport.KryptoniteFoodsRecommendation.MapRecommendations(draft.DietAndNutritionReport.KryptoniteFoodsRecommendation);
        report.DietAndNutritionReport.SuperFoodsRecommendation.MapRecommendations(draft.DietAndNutritionReport.SuperFoodsRecommendation);
        report.DietAndNutritionReport.VitaminsAndMicronutrientsRecommendation.MapRecommendations(draft.DietAndNutritionReport.VitaminsAndMicronutrientsRecommendation);
        report.ExerciseAndRecoveryReport.Recommendation.MapRecommendations(draft.ExerciseAndRecoveryReport.Recommendation);
        report.SleepReport.Recommendation.MapRecommendations(draft.SleepReport.Recommendation);
        report.MicrobiomeReport.Recommendation.MapRecommendations(draft.MicrobiomeReport.Recommendation);
        report.NeurobehavioralReport.Recommendation.MapRecommendations(draft.NeurobehavioralReport.Recommendation);
        report.LongevityReport.DementiaRecommendation.MapRecommendations(draft.LongevityReport.DementiaRecommendation);
        report.LongevityReport.InflammationRecommendation.MapRecommendations(draft.LongevityReport.InflammationRecommendation);
        report.LongevityReport.CardiovascularRecommendation.MapRecommendations(draft.LongevityReport.CardiovascularRecommendation);
        report.LongevityReport.InsulinResistanceRecommendation.MapRecommendations(draft.LongevityReport.InsulinResistanceRecommendation);
        report.LongevityReport.Recommendation.MapRecommendations(draft.LongevityReport.Recommendation);
        report.OverallReport.SupplementsRecommendation.MapRecommendations(draft.OverallReport.SupplementsRecommendation);
            
        return await _healthReportService.UpdateAsync(report);
    }

    /// <summary>
    /// Validates required data for health report generation
    /// </summary>
    /// <param name="aggregator"></param>
    /// <exception cref="AppException"></exception>
    private void RequiredDataValidation(InputsAggregator aggregator)
    {
        if (aggregator.Dna.All(x => x.Genotype == "XX"))
        {
            throw new AppException(HttpStatusCode.BadRequest,
                "Cannot generate health report. Patient does not have DNA results.");
        }
    }

    /// <summary>
    /// Fetches latest health report
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    private async Task<HealthReport?> FetchLatestHealthReport(int patientId)
    {
        try
        {
            return await _healthReportService.GetLatestAsync(patientId);
        }
        catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
        
    #endregion
}
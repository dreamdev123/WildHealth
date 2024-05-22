using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.HealthReports;
using WildHealth.Application.Extensions.HealthReports;
using WildHealth.Application.Services.HealthReports;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Shared.Exceptions;
using WildHealth.Report.Services.RecommendationsGenerator;
using MediatR;
using WildHealth.Application.Events.Reports;

namespace WildHealth.Application.CommandHandlers.HealthReports;

public class EditHealthReportCommandHandler : IRequestHandler<EditHealthReportCommand, HealthReport>
{
    private readonly IRecommendationsGenerator _recommendationsGenerator;
    private readonly IHealthReportService _healthReportService;
    private readonly IPermissionsGuard _permissionsGuard;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    public EditHealthReportCommandHandler(
        IRecommendationsGenerator recommendationsGenerator,
        ILogger<EditHealthReportCommandHandler> logger,
        IHealthReportService healthReportService,
        IPermissionsGuard permissionsGuard,
        IMediator mediator)
    {
        _recommendationsGenerator = recommendationsGenerator;
        _healthReportService = healthReportService;
        _permissionsGuard = permissionsGuard;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<HealthReport> Handle(EditHealthReportCommand command, CancellationToken cancellationToken)
    {
        var report = await _healthReportService.GetAsync(command.Id, command.PatientId);
            
        _permissionsGuard.AssertPermissions(report);
        
        if (!report.CanBeModified())
        {
            _logger.LogWarning($"Report with [Id] = {report.Id} can not be modified.");
            var exceptionParameter = new AppException.ExceptionParameter(nameof(command.Id), command.Id);
            throw new AppException(HttpStatusCode.BadRequest, "Health Report can not be modified", exceptionParameter);
        }
        
        report.DietAndNutritionReport.MacronutrientRecommendation.MapRecommendations(command.MacronutrientRecommendation);
        report.DietAndNutritionReport.MethylationRecommendation.MapRecommendations(command.MethylationRecommendation);
        report.DietAndNutritionReport.CompleteDietRecommendation.MapRecommendations(command.CompleteDietRecommendation);
        report.DietAndNutritionReport.KryptoniteFoodsRecommendation.MapRecommendations(command.KryptoniteFoodsRecommendation);
        report.DietAndNutritionReport.SuperFoodsRecommendation.MapRecommendations(command.SuperFoodsRecommendation);
        report.DietAndNutritionReport.VitaminsAndMicronutrientsRecommendation.MapRecommendations(command.VitaminsAndMicronutrientsRecommendation);
        report.ExerciseAndRecoveryReport.Recommendation.MapRecommendations(command.ExerciseAndRecoveryRecommendation);
        report.SleepReport.Recommendation.MapRecommendations(command.SleepRecommendation);
        report.MicrobiomeReport.Recommendation.MapRecommendations(command.MicrobiomeRecommendation);
        report.NeurobehavioralReport.Recommendation.MapRecommendations(command.NeurobehavioralRecommendation);
        report.LongevityReport.DementiaRecommendation.MapRecommendations(command.DementiaRecommendation);
        report.LongevityReport.InflammationRecommendation.MapRecommendations(command.InflammationRecommendation);
        report.LongevityReport.CardiovascularRecommendation.MapRecommendations(command.CardiovascularRecommendation);
        report.LongevityReport.InsulinResistanceRecommendation.MapRecommendations(command.InsulinResistanceRecommendation);
        report.LongevityReport.Recommendation.MapRecommendations(command.LongevityRecommendation);
        report.OverallReport.SupplementsRecommendation.MapRecommendations(command.SupplementsRecommendation);

        await _recommendationsGenerator.UpdateRecommendationsAsync(command.Recommendations.ToArray(), report);

        // update Apoe information for HealthSummary
        var reportRegenerateEvent = new HealthReportUpdatedEvent(command.PatientId, report );
        await _mediator.Publish(reportRegenerateEvent, cancellationToken);
       
        
        return report;
    }
}
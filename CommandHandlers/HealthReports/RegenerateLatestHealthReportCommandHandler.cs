using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.HealthReports;
using WildHealth.Application.Services.HealthReports;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Report.Services.RecommendationsGenerator;
using MediatR;
using WildHealth.Application.Events.Reports;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;

namespace WildHealth.Application.CommandHandlers.HealthReports;

public class RegenerateLatestHealthReportCommandHandler : IRequestHandler<RegenerateLatestHealthReportCommand, HealthReport>
{
    private readonly IRecommendationsGenerator _recommendationsGenerator;
    private readonly IHealthReportService _healthReportService;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly IPermissionsGuard _permissionsGuard;
    private readonly IInputsService _inputsService;
    private readonly IMediator _mediator;

    public RegenerateLatestHealthReportCommandHandler(
        IRecommendationsGenerator recommendationsGenerator,
        IHealthReportService healthReportService, 
        IFeatureFlagsService featureFlagsService,
        IPermissionsGuard permissionsGuard,
        IInputsService inputsService,
        IMediator mediator)
    {
        _recommendationsGenerator = recommendationsGenerator;
        _healthReportService = healthReportService;
        _featureFlagsService = featureFlagsService;
        _permissionsGuard = permissionsGuard;
        _inputsService = inputsService;
        _mediator = mediator;
    }

    public async Task<HealthReport> Handle(RegenerateLatestHealthReportCommand command, CancellationToken cancellationToken)
    {
        var report = await _healthReportService.GetLatestAsync(command.PatientId);
            
        _permissionsGuard.AssertPermissions(report);
            
        var aggregator = await _inputsService.GetAggregatorAsync(command.PatientId, true);
            
        var healthReport = await _healthReportService.GenerateAsync(report, aggregator);

        // update Apoe information for HealthSummary
        var reportRegenerateEvent = new HealthReportUpdatedEvent(command.PatientId, healthReport);
        
        await _mediator.Publish(reportRegenerateEvent, cancellationToken);

        if (_featureFlagsService.GetFeatureFlag(FeatureFlags.RecommendationsV2))
        {
            await _recommendationsGenerator.SetRecommendationsV2Async(
                recommendations: command.Recommendations,
                healthReport: healthReport
            );
        }
        else
        {
            await _recommendationsGenerator.SetRecommendationsAsync(
                recommendations: command.Recommendations,
                healthReport: healthReport,
                aggregator: aggregator
            );
        }

        return healthReport;
    }
}
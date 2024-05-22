using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.PatientEngagements;
using WildHealth.Application.Domain.PatientEngagements.Flows;
using WildHealth.Application.Domain.PatientEngagements.Services;
using WildHealth.Application.Extensions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Engagement;

namespace WildHealth.Application.Domain.PatientEngagements.CommandHandlers;

public class RunPatientEngagementScannersCommandHandler : IRequestHandler<RunPatientEngagementScannersCommand>
{
    private readonly IEngagementScannerAggregator _engagementScannerAggregator;
    private readonly ILogger<RunPatientEngagementScannersCommandHandler> _logger;
    private readonly IPatientEngagementService _engagementService;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly MaterializeFlow _materialize;

    public RunPatientEngagementScannersCommandHandler(
        IEngagementScannerAggregator engagementScannerAggregator, 
        ILogger<RunPatientEngagementScannersCommandHandler> logger, 
        IPatientEngagementService engagementService,
        IFeatureFlagsService featureFlagsService,
        MaterializeFlow materialize)
    {
        _engagementScannerAggregator = engagementScannerAggregator;
        _engagementService = engagementService;
        _materialize = materialize;
        _featureFlagsService = featureFlagsService;
        _logger = logger;
    }

    public async Task Handle(RunPatientEngagementScannersCommand command, CancellationToken cancellationToken)
    {
        var criteria = await _engagementService.GetNotDisabledCriteria(command.Assignee);
        var scanners = criteria.Select(EngagementCriteriaScannerFactory.CreateScanner);

        var (qualifiedForEngagement, timeSpent1) = await _engagementScannerAggregator
            .Aggregate(scanners, command.UtcNow)
            .ToListAsync(cancellationToken)
            .Measure();
        _logger.LogInformation($"{qualifiedForEngagement.Count} rows found to consider for engagement. Time Spent: {timeSpent1.TotalSeconds}");

        var (engagementHistory, timeSpent2) = await _engagementService.GetHistory(PatientIds(qualifiedForEngagement)).Measure();
        _logger.LogInformation($"{engagementHistory.Count} history rows found for given patients. Time Spent: {timeSpent2.TotalSeconds}");

        var (createResult, timeSpent3) = await new CreatePatientEngagementsFlow(qualifiedForEngagement, engagementHistory, IsNotificationsEnabledForUser, command.UtcNow, _logger.LogInformation)
            .Materialize(_materialize)
            .Measure();
        _logger.LogInformation($"{createResult.SelectMany<PatientEngagement>().Count()} New Patient Engagements have been created. Time Spent: {timeSpent3.TotalSeconds}");
    }
    
    private bool IsNotificationsEnabledForUser(Guid universalUserId) => 
        _featureFlagsService.GetFeatureFlag(FeatureFlags.PatientEngagementsNotifications, universalUserId);

    private int[] PatientIds(List<EngagementScannerAggregateResult> qualifiedForEngagement) => qualifiedForEngagement
        .Select(x => x.PatientId)
        .ToArray();
}
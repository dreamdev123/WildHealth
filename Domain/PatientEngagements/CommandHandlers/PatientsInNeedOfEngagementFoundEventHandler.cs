using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Domain.PatientEngagements.Flows;
using WildHealth.Application.Domain.PatientEngagements.Services;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Settings;

namespace WildHealth.Application.Domain.PatientEngagements.CommandHandlers;

public record PatientsInNeedOfEngagementFoundEvent : INotification;

public class PatientsInNeedOfEngagementFoundEventHandler : INotificationHandler<PatientsInNeedOfEngagementFoundEvent>
{
    private readonly ILogger<PatientsInNeedOfEngagementFoundEventHandler> _logger;
    private readonly IPatientEngagementService _engagementService;
    private readonly MaterializeFlow _materializer;
    private readonly ISettingsManager _settingsManager;
    private readonly AppOptions _appOptions;
    
    public PatientsInNeedOfEngagementFoundEventHandler(
        IPatientEngagementService engagementService, 
        MaterializeFlow materializer, 
        ILogger<PatientsInNeedOfEngagementFoundEventHandler> logger, 
        ISettingsManager settingsManager, 
        IOptions<AppOptions> options)
    {
        _engagementService = engagementService;
        _materializer = materializer;
        _logger = logger;
        _settingsManager = settingsManager;
        _appOptions = options.Value;
    }

    public async Task Handle(PatientsInNeedOfEngagementFoundEvent notification, CancellationToken cancellationToken)
    {
        var patientEngagements = await _engagementService.GetPending();
        var dashboardUrlLookup = await GetDashboardUrlLookup(patientEngagements);

        foreach (var pending in patientEngagements)
        {
            var practiceId = pending.EngagementCriteria.PracticeId;
            if (!dashboardUrlLookup.ContainsKey(practiceId))
            {
                _logger.LogError($"Couldn't find dashboard URL for Practice - {practiceId}");
                continue;
            }
            
            var dashboardUrl = dashboardUrlLookup[practiceId];
            var result = await new NotifyPatientsInNeedOfEngagementFlow(pending, dashboardUrl, DateTime.UtcNow)
                .Materialize(_materializer)
                .ToTry();

            result.DoIfError(ex => _logger.LogError("Couldn't act upon patient engagement item {PatientEngagement}. Error: {Error}", pending, ex));
        }
    }

    private async Task<Dictionary<int, string>> GetDashboardUrlLookup(List<PatientEngagement> patientEngagements)
    {
        var practices = patientEngagements.Select(pe => pe.EngagementCriteria.PracticeId).Distinct();
        var result = await FetchDashboardUrls().ToListAsync();
        
        return result.ToDictionary(x => x.practiceId, x => x.dashboardUrl);
        
        async IAsyncEnumerable<(int practiceId, string dashboardUrl)> FetchDashboardUrls()
        {
            foreach (var practiceId in practices)
            {
                var settings = await _settingsManager.GetSettings(new []{SettingsNames.General.ApplicationBaseUrl}, practiceId);
                var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
                yield return (practiceId, string.Format(_appOptions.DashboardUrl, applicationUrl));
            }
        }
    }
}
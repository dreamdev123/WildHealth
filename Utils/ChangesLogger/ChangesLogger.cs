using System;
using System.Threading.Tasks;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.Shared.Data.ChangesLogger;
using WildHealth.Shared.Data.Models;
using WildHealth.IntegrationEvents.Audits;
using WildHealth.IntegrationEvents.Audits.Payloads;

namespace WildHealth.Application.Utils.ChangesLogger;

/// <summary>
/// <see cref="IChangesLogger"/>
/// </summary>
public class ChangesLogger : IChangesLogger
{
    private readonly IEventBus _eventBus;
    private readonly IFeatureFlagsService _featureFlagsService;

    public ChangesLogger(IEventBus eventBus, IFeatureFlagsService featureFlagsService)
    {
        _eventBus = eventBus;
        _featureFlagsService = featureFlagsService;
    }
 
    /// <summary>
    /// <see cref="IChangesLogger.LogAsync"/>
    /// </summary>
    /// <param name="changes"></param>
    /// <param name="byUserId"></param>
    /// <returns></returns>
    public async Task LogAsync(ChangesModel[] changes, int byUserId)
    {
        if (_featureFlagsService.GetFeatureFlag(FeatureFlags.AuditIntegrationEvents))
        {
            var eventId = Guid.NewGuid();

            foreach (var change in changes)
            {
                try
                {
                    await _eventBus.Publish(new AuditIntegrationEvent(
                        payload: new AuditPayload(
                            eventId: eventId,
                            recordId: change.Key, 
                            recordType: change.Entity,
                            recordState: change.State,
                            recordPropertiesPrior: change.OriginalValue,
                            recordPropertiesCurrent: change.CurrentValue,
                            auditedByUserId: byUserId),
                        // patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        patient: new PatientMetadataModel(0, "0"),
                        eventDate: DateTime.UtcNow)
                    );
                }
                catch (Exception)
                {
                    // empty catch because we don`t want to interrupt process, it is just logs 
                }
            }
        }
    }
}
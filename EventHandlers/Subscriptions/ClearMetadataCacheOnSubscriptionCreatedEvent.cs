using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Common.Models.Patients;
using WildHealth.Shared.DistributedCache.Services;

namespace WildHealth.Application.EventHandlers.Subscriptions;

public class ClearMetadataCacheOnSubscriptionCreatedEvent : INotificationHandler<SubscriptionCreatedEvent>
{
    private readonly IWildHealthSpecificCacheService<PatientMetaDataModel, PatientMetaDataModel> _cacheService;


    public ClearMetadataCacheOnSubscriptionCreatedEvent(IWildHealthSpecificCacheService<PatientMetaDataModel, PatientMetaDataModel> cacheService)
    {
        _cacheService = cacheService;
    }

    public Task Handle(SubscriptionCreatedEvent notification, CancellationToken cancellationToken)
    {
        var patientId = notification.Patient.GetId();

        _cacheService.RemoveKey(patientId.GetHashCode().ToString());
        
        return Task.CompletedTask;
    }
}
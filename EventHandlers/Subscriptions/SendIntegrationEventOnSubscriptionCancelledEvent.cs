using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Patients;
using WildHealth.IntegrationEvents.Patients.Payloads;
using MediatR;
using WildHealth.Application.Services.HealthScore;
using WildHealth.Domain.Enums;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.EventHandlers.Subscriptions
{
    public class SendIntegrationEventOnSubscriptionCancelledEvent : INotificationHandler<SubscriptionCancelledEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly IHealthScoreService _healthScoreService;

        public SendIntegrationEventOnSubscriptionCancelledEvent(IHealthScoreService healthScoreService)
        {
            _healthScoreService = healthScoreService;
            _eventBus = EventBusProvider.Get();
        }

        public async Task Handle(SubscriptionCancelledEvent notification, CancellationToken cancellationToken)
        {
            var patient = notification.Patient;
            var subscription = notification.Subscription;
            var addOns = subscription.PaymentPrice?.PaymentPeriod?.PaymentPlan?.PaymentPlanAddOns;
            var healthScore = await _healthScoreService.GetPatientHealthScore(patient.GetId().ToString()).ToTry();
            
            await _eventBus.Publish(new PatientIntegrationEvent(
                payload: new PatientSubscriptionCanceledPayload(
                    locationId: patient.LocationId,
                    planType: subscription.PaymentPrice?.GetDisplayName(),
                    price: subscription.PaymentPrice?.GetPrice().ToString(CultureInfo.InvariantCulture), 
                    dnaKit: addOns != null && addOns.Any(o => OrderType.Dna.Equals(o.AddOn.OrderType)),
                    epiKit: addOns != null && addOns.Any(o => OrderType.Epigenetic.Equals(o.AddOn.OrderType)),
                    referral: patient.PatientLeadSource?.LeadSource?.Name,
                    referralOtherInput: patient.PatientLeadSource?.LeadSource?.IsOther == true 
                        ? patient.PatientLeadSource?.OtherSource 
                        : string.Empty,
                    cadence: subscription.DetermineCadence(),
                    planPlatform: subscription.DeterminePlatform(),
                    cancelReason: notification.Subscription.CancellationReasonType.ToName(),
                    additionalReason: notification.Subscription.CancellationReason,
                    healthScore: healthScore.Select(x => x.PatientScore.Score.ToString()).ValueOr(string.Empty)
                ),
                patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                practice: new PracticeMetadataModel(patient.User.PracticeId),
                eventDate: patient.CreatedAt), cancellationToken);
        }
    }
}
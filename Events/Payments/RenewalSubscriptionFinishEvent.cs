using System.Collections.Generic;
using WildHealth.Common.Models.Subscriptions;
using MediatR;

namespace WildHealth.Application.Events.Payments
{
    public class RenewalSubscriptionFinishEvent : INotification
    {
        public IEnumerable<RenewSubscriptionReportModel> RenewedSubscription { get; }

        public int PracticeId { get; }

        public RenewalSubscriptionFinishEvent(
            IEnumerable<RenewSubscriptionReportModel> renewedSubscription,
            int practiceId)
        {
            RenewedSubscription = renewedSubscription;
            PracticeId = practiceId;
        }
    }
}
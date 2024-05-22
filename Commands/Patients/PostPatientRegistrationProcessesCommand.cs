using FluentValidation;
using MediatR;
using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.IntegrationEvents.Patients;

namespace WildHealth.Application.Commands.Patients
{
    public record PostPatientRegistrationProcessesCommand : IRequest, IValidatabe
    {
        public int PracticeId { get; set; }
        public int PatientId { get; set;}
        public int? EmployeeId { get; }
        public int? LinkedEmployeeId { get; }
        public int LocationId { get; set;}
        public int PaymentPriceId { get; set; }
        public int SubscriptionId { get; set; }
        public string EmployerProductKey { get; set; }
        public bool IsTrialPlan { get; }
        public IEnumerable<int> AddonIds { get; set; }
        public int? FounderId { get; set; }
        public string InviteCode { get; set; }
        public int? LeadSourceId { get; set; }
        public string OtherLeadSource { get; set; }
        public string PodcastSource { get; set; }
        public PatientIntegrationEvent OriginatedFromEvent { get; set; }

        public PostPatientRegistrationProcessesCommand(
            int practiceId,
            int patientId,
            int? employeeId,
            int? linkedEmployeeId,
            int locationId,
            int paymentPriceId,
            int subscriptionId,
            string employerProductKey,
            bool isTrialPlan,
            int? founderId,
            string inviteCode,
            int? leadSourceId,
            string otherLeadSource,
            string podcastSource,
            IEnumerable<int> addonIds,
            PatientIntegrationEvent originatedFromEvent)
        {
            PracticeId = practiceId;
            PatientId = patientId;
            EmployeeId = employeeId;
            LinkedEmployeeId = linkedEmployeeId;
            LocationId = locationId;
            PaymentPriceId = paymentPriceId;
            SubscriptionId = subscriptionId;
            EmployerProductKey = employerProductKey;
            IsTrialPlan = isTrialPlan;
            FounderId = founderId;
            InviteCode = inviteCode;
            LeadSourceId = leadSourceId;
            OtherLeadSource = otherLeadSource;
            PodcastSource = podcastSource;
            AddonIds = addonIds;
            OriginatedFromEvent = originatedFromEvent;
        }

        #region validation

        private class Validator : AbstractValidator<PostPatientRegistrationProcessesCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.SubscriptionId).GreaterThan(0);
                RuleFor(x => x.EmployeeId).GreaterThan(0).When(x => x.EmployeeId.HasValue);
                RuleFor(x => x.LinkedEmployeeId).GreaterThan(0).When(x => x.LinkedEmployeeId.HasValue);
                RuleFor(x => x.LocationId).GreaterThan(0);
                RuleFor(x => x.PaymentPriceId).GreaterThan(0);
                RuleFor(x => x.SubscriptionId).GreaterThan(0);
            }
        }

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        #endregion
    }
}

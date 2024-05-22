using FluentValidation;
using MediatR;
using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.LeadSources;

namespace WildHealth.Application.Events.Patients
{
    public record PatientRegisteredEvent(int PracticeId,
        int PatientId,
        string UniversalUserId,
        int? EmployeeId,
        int? LinkedEmployeeId,
        int LocationId,
        int PaymentPriceId,
        int SubscriptionId,
        string? EmployerProductKey,
        bool IsTrialPlan,
        string InviteCode,
        int? FounderId,
        PatientLeadSourceModel? LeadSource,
        IEnumerable<int> AddonIds) : INotification, IValidatabe
    {
        #region validation

        private class Validator : AbstractValidator<PatientRegisteredEvent>
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
                RuleFor(x => x.AddonIds).NotNull();
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

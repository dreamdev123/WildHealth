using System.Collections.Generic;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Payments
{
    public class SendConfirmationEmailCommand : IRequest, IValidatabe
    {
        public Patient Patient { get; }

        public Subscription NewSubscription { get; }

        public Subscription PreviousSubscription { get; }

        public IEnumerable<int> PatientAddOnIds { get; }


        public SendConfirmationEmailCommand(
            Patient patient, 
            Subscription newSubscription,
            Subscription previousSubscription,
            IEnumerable<int> patientAddOnIds)
        {
            Patient = patient;
            NewSubscription = newSubscription;
            PreviousSubscription = previousSubscription;
            PatientAddOnIds = patientAddOnIds;
        }

        #region validation

        private class Validator : AbstractValidator<SendConfirmationEmailCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Patient).NotNull();
                RuleFor(x => x.PreviousSubscription).NotNull();
                RuleFor(x => x.NewSubscription).NotNull();
                RuleFor(x => x.PatientAddOnIds).NotNull();
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

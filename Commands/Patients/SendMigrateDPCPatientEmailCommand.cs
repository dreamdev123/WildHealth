using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using MediatR;
using WildHealth.Application.Commands._Base;
using FluentValidation;

namespace WildHealth.Application.Commands.Patients
{
    public class SendMigrateDPCPatientEmailCommand : IRequest, IValidatabe
    {
        public Patient Patient { get; }

        public Subscription Subscription { get; }

        public SendMigrateDPCPatientEmailCommand(Patient patient, Subscription subscription)
        {
            Patient = patient;
            Subscription = subscription;
        }

        #region validation

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<SendMigrateDPCPatientEmailCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Patient).NotNull();
                RuleFor(x => x.Subscription).NotNull();
            }
        }

        #endregion
    }
}

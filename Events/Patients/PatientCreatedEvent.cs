using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Events.Patients
{
    public record PatientCreatedEvent(
        int PatientId,  
        int SubscriptionId,
        int[] SelectedAddOnIds) : INotification, IValidatabe
    {
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<PatientCreatedEvent>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
            }
        }
    }
}
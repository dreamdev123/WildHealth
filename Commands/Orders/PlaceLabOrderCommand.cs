using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class PlaceLabOrderCommand : IRequest, IValidatabe
    {
        public Patient Patient { get; }
        
        public int[] AddOnIds { get; }
        
        public PlaceLabOrderCommand(Patient patient, int[] addOnIds)
        {
            Patient = patient;
            AddOnIds = addOnIds;
        }

        #region private

        private class Validator : AbstractValidator<PlaceLabOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Patient).NotNull();
                RuleFor(x => x.AddOnIds).NotNull().NotEmpty();
                RuleForEach(x => x.AddOnIds).GreaterThan(0);
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
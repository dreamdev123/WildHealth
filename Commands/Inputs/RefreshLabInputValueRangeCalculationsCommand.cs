using System;
using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Inputs
{
    public class RefreshLabInputValueRangeCalculationsCommand : IRequest, IValidatabe
    {
        public int PatientId { get; }
        
        public DateTime? OnDate { get; }
        
        public RefreshLabInputValueRangeCalculationsCommand(
            int patientId, 
            DateTime? onDate=null)
        {
            PatientId = patientId;
            OnDate = onDate;
        }

        #region validation

        private class Validator : AbstractValidator<RefreshLabInputValueRangeCalculationsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
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
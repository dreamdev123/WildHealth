using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models._Base;

namespace WildHealth.Application.Commands.Inputs
{
    public class SetPatientMesaValueOnInputCommand:  IRequest, IValidatable
    {
        public int PatientId { get; }
        

        public SetPatientMesaValueOnInputCommand(
            int patientId)
        {
            PatientId = patientId;
        
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

        private class Validator : AbstractValidator<SetPatientMesaValueOnInputCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId)
                    .GreaterThan(0);
            }
        }

        #endregion
    }
}
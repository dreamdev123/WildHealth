using FluentValidation;
using System.Collections.Generic;
using WildHealth.Integration.Models.Patients;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Patients
{
    public class CreatePaymentIntegrationAccountCommand : IRequest<IEnumerable<PatientCreatedModel>>, IValidatabe
    {
        public int PatientId { get; }
        
        public CreatePaymentIntegrationAccountCommand(int patientId)
        {
            PatientId = patientId;
        }

        #region validation

        private class Validator : AbstractValidator<CreatePaymentIntegrationAccountCommand>
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

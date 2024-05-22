using MediatR;
using System.Collections.Generic;
using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Commands.Patients
{
    public class StartBatchPatientRegistrationCommand : IRequest<IEnumerable<Patient>>, IValidatabe
    {
        public IEnumerable<StartPatientRegistrationModel> Patients { get; }

        public StartBatchPatientRegistrationCommand(IEnumerable<StartPatientRegistrationModel> patients)
        {
            Patients = patients;
        }
        
        #region validation

        private class Validator : AbstractValidator<StartBatchPatientRegistrationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Patients).NotNull().NotEmpty();
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

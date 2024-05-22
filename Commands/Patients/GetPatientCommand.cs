using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Patients;
using MediatR;

namespace WildHealth.Application.Commands.Patients
{
    public class GetPatientCommand : IRequest<PatientModel>, IValidatabe
    {
        public int Id { get; }
        
        public GetPatientCommand(int id)
        {
            Id = id;
        }

        #region validation

        private class Validator : AbstractValidator<GetPatientCommand>
        {
            public Validator()
            {
#pragma warning disable CS0618
                RuleFor(x => x.Id).Cascade(CascadeMode.StopOnFirstFailure).GreaterThan(0);  // TODO: resolve obsolete CascadeMode
#pragma warning restore CS0618
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

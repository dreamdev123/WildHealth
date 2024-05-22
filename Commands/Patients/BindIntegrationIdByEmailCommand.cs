using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Commands.Patients
{
    public class BindIntegrationIdByEmailCommand : IRequest<Patient>, IValidatabe
    {
        public string Email { get; }

        public BindIntegrationIdByEmailCommand(string email)
        {
            Email = email;
        }
        
        #region validation

        private class Validator : AbstractValidator<BindIntegrationIdByEmailCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
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
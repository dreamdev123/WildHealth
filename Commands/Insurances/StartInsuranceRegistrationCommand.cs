using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Auth;

namespace WildHealth.Application.Commands.Insurances
{
    public class StartInsuranceRegistrationCommand : IRequest<AuthenticationResultModel>, IValidatabe
    {
        public string Email { get; }
        
        public string Password { get; }
        
        public int PracticeId { get; }
        
        public StartInsuranceRegistrationCommand(
            string email, 
            string password, 
            int practiceId)
        {
            Email = email;
            Password = password;
            PracticeId = practiceId;
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
        
        private class Validator : AbstractValidator<StartInsuranceRegistrationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
                RuleFor(x => x.Password).NotNull().NotEmpty();
                RuleFor(x => x.PracticeId).GreaterThan(0);
            }
        }
        
        #endregion
    }
}
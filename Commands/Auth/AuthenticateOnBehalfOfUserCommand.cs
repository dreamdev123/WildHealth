using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Auth;
using MediatR;

namespace WildHealth.Application.Commands.Auth
{
    public class AuthenticateOnBehalfOfUserCommand : IRequest<AuthenticationResultModel>, IValidatabe
    {
        public int? UserId { get; }
        
        public int? PatientId { get; }
        
        public int? EmployeeId { get; }

        public AuthenticateOnBehalfOfUserCommand(
            int? userId,
            int? patientId,
            int? employeeId)
        {
            UserId = userId;
            PatientId = patientId;
            EmployeeId = employeeId;
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

        private class Validator : AbstractValidator<AuthenticateOnBehalfOfUserCommand>
        {
            public Validator()
            {
                RuleFor(command => command.UserId).GreaterThan(0).When(command => command.UserId.HasValue);
                RuleFor(command => command.PatientId).GreaterThan(0).When(command => command.PatientId.HasValue);
                RuleFor(command => command.EmployeeId).GreaterThan(0).When(command => command.EmployeeId.HasValue);
            }
        }
        
        #endregion
    }
}
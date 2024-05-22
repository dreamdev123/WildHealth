using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Commands.Employees
{
    public class CreateFellowCommand : IRequest<Fellow>, IValidatabe
    {
        public int RosterId { get; }
        
        public string FirstName { get; }
        
        public string LastName { get; }
        
        public string Email { get; }
        
        public string PhoneNumber { get; }

        public string Credentials { get; }

        public CreateFellowCommand(
            int rosterId,
            string firstName, 
            string lastName, 
            string email,
            string phoneNumber,
            string credentials)
        {
            RosterId = rosterId;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            Credentials = credentials;
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

        private class Validator : AbstractValidator<CreateFellowCommand>
        {
            public Validator()
            {
                RuleFor(x => x.FirstName).NotEmpty();
                RuleFor(x => x.LastName).NotEmpty();
                RuleFor(x => x.PhoneNumber).NotEmpty();
                RuleFor(x => x.Email).NotEmpty().EmailAddress();
            }
        }

        #endregion
    }
}
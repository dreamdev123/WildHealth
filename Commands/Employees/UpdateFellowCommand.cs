using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Commands.Employees
{
    public class UpdateFellowCommand : IRequest<Fellow>, IValidatabe
    {
        public int Id { get; }
        
        public string FirstName { get; }
        
        public string LastName { get; }

        public string Email { get; }
        
        public string PhoneNumber { get; }

        public string Credentials { get; }

        public UpdateFellowCommand(
            int id,
            string firstName, 
            string lastName, 
            string email,
            string phoneNumber,
            string credentials)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            Email = email;
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

        private class Validator : AbstractValidator<UpdateFellowCommand>
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
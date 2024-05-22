using FluentValidation;
using WildHealth.Domain.Entities.Users;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models._Base;

namespace WildHealth.Application.Events.Users
{
    public class UserAuthenticatedEvent : INotification, IValidatable
    {
        public User User { get; }
        
        public UserAuthenticatedEvent(
            User user
        )
        {
            User = user;
        }
        
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<UserAuthenticatedEvent>
        {
            public Validator()
            {
                RuleFor(x => x.User).NotNull();
            }
        }
        
    }
}
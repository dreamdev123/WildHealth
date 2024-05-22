using FluentValidation;
using WildHealth.Domain.Entities.Users;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models._Base;

namespace WildHealth.Application.Events.Users
{
    public class UserUpdatedEvent : INotification, IValidatable
    {
        public User User { get; }
        
        public UserUpdatedEvent(
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

        private class Validator : AbstractValidator<UserUpdatedEvent>
        {
            public Validator()
            {
                RuleFor(x => x.User).NotNull();
                
            }
        }
        
    }
}
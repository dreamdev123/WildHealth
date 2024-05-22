using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Users;
using MediatR;

namespace WildHealth.Application.Commands.Users
{
    public class FetchUserCommand : IRequest<UserModel>, IValidatabe
    {
        public string ConversationIdentity { get; }
        
        public FetchUserCommand(string conversationIdentity)
        {
            ConversationIdentity = conversationIdentity;
        }

        #region validation

        private class Validator : AbstractValidator<FetchUserCommand>
        {
            public Validator()
            {
#pragma warning disable CS0618
                RuleFor(x => x.ConversationIdentity).Cascade(CascadeMode.StopOnFirstFailure).NotNull().NotEmpty(); // TODO: resolve obsolete CascadeMode
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

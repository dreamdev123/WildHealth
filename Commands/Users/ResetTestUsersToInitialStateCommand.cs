using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Users
{
    public class ResetTestUsersToInitialStateCommand : IRequest<string[]>, IValidatabe
    {
        public string[] Emails { get;  }
        
        public ResetTestUsersToInitialStateCommand(string[] emails)
        {
            Emails = emails;
        }

        #region validation

        private class Validator : AbstractValidator<ResetTestUsersToInitialStateCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Emails).NotNull().NotEmpty();
                RuleForEach(x => x.Emails)
                    .EmailAddress()
                    .Must(x => x.Contains("test"))
                    .Must(x => x.Contains("@byom.de") || x.Contains("@yopmail.com"));
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
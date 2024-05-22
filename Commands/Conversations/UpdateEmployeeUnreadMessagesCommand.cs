using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class UpdateEmployeeUnreadMessagesCommand : IRequest, IValidatabe
    {
        public UpdateEmployeeUnreadMessagesCommand() {}

        #region validation

        private class Validator : AbstractValidator<UpdateEmployeeUnreadMessagesCommand>
        {
            public Validator() { }
        }

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => true;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        #endregion
    }
}

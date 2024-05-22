using FluentValidation;
using MediatR;
using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// Command return conversations which are support and open. And employee have same practices for the theme of conversation.
    /// </summary>
    public class GetSupportSubmissionsCommand : IRequest<IEnumerable<Conversation>>, IValidatabe
    {
        public int EmployeeId { get; }

        public GetSupportSubmissionsCommand(int employeeId)
        {
            EmployeeId = employeeId;
        }

        #region validation

        private class Validator : AbstractValidator<GetSupportSubmissionsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeId).GreaterThan(0);
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

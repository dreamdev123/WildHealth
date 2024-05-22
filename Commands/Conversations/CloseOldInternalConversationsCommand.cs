using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Conversations;

public class CloseOldInternalConversationsCommand : IRequest, IValidatabe
{
    public int ConversationsOlderThanDays { get; set; }

    public CloseOldInternalConversationsCommand(
        int conversationsOlderThanDays)
    {
        ConversationsOlderThanDays = conversationsOlderThanDays;
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

    private class Validator : AbstractValidator<CloseOldInternalConversationsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationsOlderThanDays).GreaterThan(0);
        }
    }

    #endregion
}
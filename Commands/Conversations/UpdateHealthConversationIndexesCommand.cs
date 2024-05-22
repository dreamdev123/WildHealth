
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Conversations;

/// <summary>
/// This command provide updating scheduled message for conversation from employee
/// </summary>
public class UpdateHealthConversationIndexesCommand : IRequest, IValidatabe
{
    public UpdateHealthConversationIndexesCommand()
    {
    }

    #region validation

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new UpdateHealthConversationIndexesCommand.Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new UpdateHealthConversationIndexesCommand.Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<UpdateHealthConversationIndexesCommand>
    {
        public Validator()
        {
            
        }
    }

    #endregion
}
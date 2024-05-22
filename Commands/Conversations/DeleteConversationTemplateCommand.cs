using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class DeleteConversationTemplateCommand : IRequest<ConversationTemplate>, IValidatabe
{
    public int Id { get; }
    
    public DeleteConversationTemplateCommand(int id)
    {
        Id = id;
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

    private class Validator : AbstractValidator<DeleteConversationTemplateCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    #endregion
}
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class CreateConversationTemplateCommand : IRequest<ConversationTemplate>, IValidatabe
{
    public string Name { get; }
    
    public string Description { get; }
    
    public string Text { get; }
    
    public int Order { get; }
    
    public ConversationType Type { get; }
    
    public CreateConversationTemplateCommand(
        string name, 
        string description, 
        string text, 
        int order, 
        ConversationType type)
    {
        Name = name;
        Description = description;
        Text = text;
        Order = order;
        Type = type;
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

    private class Validator : AbstractValidator<CreateConversationTemplateCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotNull().NotEmpty();
            RuleFor(x => x.Description).NotNull().NotEmpty();
            RuleFor(x => x.Text).NotNull().NotEmpty();
            RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Type).IsInEnum();
        }
    }

    #endregion
}
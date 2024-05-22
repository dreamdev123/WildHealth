using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class UpdateConversationTemplateCommand : IRequest<ConversationTemplate>, IValidatabe
{
    public int Id { get; }
    
    public string Name { get; }
    
    public string Description { get; }
    
    public string Text { get; }
    
    public int Order { get; }
    
    public ConversationType Type { get; }
    
    public UpdateConversationTemplateCommand(
        int id, 
        string name, 
        string description, 
        string text, 
        int order, 
        ConversationType type)
    {
        Id = id;
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

    private class Validator : AbstractValidator<UpdateConversationTemplateCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Name).NotNull().NotEmpty();
            RuleFor(x => x.Description).NotNull().NotEmpty();
            RuleFor(x => x.Text).NotNull().NotEmpty();
            RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Type).IsInEnum();
        }
    }

    #endregion
}
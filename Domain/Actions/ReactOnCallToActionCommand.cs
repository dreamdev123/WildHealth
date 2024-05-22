using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Actions;
using WildHealth.Domain.Enums.Actions;

namespace WildHealth.Application.Domain.Actions;

public class ReactOnCallToActionCommand : IRequest<CallToAction>, IValidatabe
{
    public int Id { get; }
    
    public ActionReactionType ReactionType { get; }
    
    public string Details { get; }
    
    public ReactOnCallToActionCommand(int id, ActionReactionType reactionType, string details)
    {
        Id = id;
        ReactionType = reactionType;
        Details = details;
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

    
    private class Validator : AbstractValidator<ReactOnCallToActionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.ReactionType).IsInEnum();
        }
    }

    #endregion
}
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Domain.ConversationAwayMessageTemplates.Commands;

public record UpdateConversationAwayMessageTemplateCommand(int Id, string Title, string Body) : IRequest, IValidatabe
{
    public bool IsValid() => new Validator().Validate(this).IsValid;
    public void Validate() => new Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<UpdateConversationAwayMessageTemplateCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Body).NotEmpty();
        }
    }
}
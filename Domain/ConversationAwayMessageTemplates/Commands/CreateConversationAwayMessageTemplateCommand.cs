using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Domain.ConversationAwayMessageTemplates.Commands;

public record CreateConversationAwayMessageTemplateCommand(string Title, string Body) : IRequest<int>, IValidatabe
{
    public bool IsValid() => new Validator().Validate(this).IsValid;
    public void Validate() => new Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<CreateConversationAwayMessageTemplateCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Body).NotEmpty();
        }
    }
};
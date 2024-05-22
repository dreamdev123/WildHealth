using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class UpdateConversationSubjectCommand : IRequest<Conversation>, IValidatabe
{
    public int Id { get; }
    
    public string NewSubject { get; }
    
    public UpdateConversationSubjectCommand(int id, string newSubject)
    {
        Id = id;
        NewSubject = newSubject;
    }
    
    #region validation

    private class Validator : AbstractValidator<UpdateConversationSubjectCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.NewSubject).NotNull().NotEmpty();
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
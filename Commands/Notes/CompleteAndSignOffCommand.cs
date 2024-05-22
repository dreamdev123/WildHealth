using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Notes;

namespace WildHealth.Application.Commands.Notes;

public class CompleteAndSignOffCommand : IRequest<Note>, IValidatabe
{
    public CompleteAndSignOffCommand(int noteId, int completedBy)
    {
        NoteId = noteId;
        CompletedBy = completedBy;
    }

    public int NoteId { get; }
    public int CompletedBy { get; }
    
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

    private class Validator : AbstractValidator<CompleteAndSignOffCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NoteId).GreaterThan(0);
            RuleFor(x => x.CompletedBy).GreaterThan(0);
        }
    }

    #endregion
}
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Notes;

namespace WildHealth.Application.Commands.Notes;

public class CancelNoteSignOffCommand : IRequest<Note>, IValidatabe
{
    public CancelNoteSignOffCommand(int noteId)
    {
        NoteId = noteId;
    }

    public int NoteId { get; }
    
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

    private class Validator : AbstractValidator<CancelNoteSignOffCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NoteId).GreaterThan(0);
        }
    }

    #endregion
}